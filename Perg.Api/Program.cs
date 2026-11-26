using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Perg.Api.Applications.Arquivos;
using Perg.Api.Applications.Assinaturas;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Cache;
using Perg.Api.Applications.Email;
using Perg.Api.Applications.Eventos;
using Perg.Api.Applications.Geocodificacao;
using Perg.Api.Applications.Importacoes;
using Perg.Api.Applications.Integracoes;
using Perg.Api.Applications.Ofertas;
using Perg.Api.Applications.Pagamentos;
using Perg.Api.Applications.Roteamento;
using Perg.Api.Applications.Services;
using Perg.Api.Applications.Sms;
using Perg.Api.Applications.TempoReal;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Domains.Enums;
using Perg.Api.Hubs;
using Perg.Api.Interfaces;
using Perg.Api.Repositories;
using Confluent.Kafka;
using Microsoft.AspNetCore.RateLimiting;
using Npgsql;
using System.Threading.RateLimiting;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
    // Enums trafegam como texto no JSON ("Admin", "Alimentacao") em vez de números
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// A connection string "Perg" fica nos Segredos do Usuário (fora do Git)
builder.Services.AddDbContext<PergDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Perg")));

builder.Services.AddScoped<GeradorTokenJwt>();
builder.Services.AddScoped<IPasswordHasher<Usuario>, PasswordHasher<Usuario>>();

builder.Services.AddScoped<IEmpresaRepository, EmpresaRepository>();
builder.Services.AddScoped<EmpresaService>();
builder.Services.AddScoped<IValorVeiculoRepository, ValorVeiculoRepository>();
builder.Services.AddScoped<TabelaValoresService>();

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddSingleton<IValidadorGoogle, ValidadorGoogle>();
builder.Services.AddScoped<EquipeService>();
builder.Services.AddScoped<RecuperacaoSenhaService>();

// Confirmação do celular do entregador. SMS pelo Twilio; sem Twilio:AccountSid nos Segredos do Usuário, só escreve no Output.
if (string.IsNullOrWhiteSpace(builder.Configuration["Twilio:AccountSid"]))
{
    builder.Services.AddSingleton<IEnvioSms, SmsConsole>();
}
else
{
    builder.Services.AddHttpClient<IEnvioSms, TwilioEnvioSms>(client =>
    {
        client.BaseAddress = new Uri("https://api.twilio.com/2010-04-01/");
        client.Timeout = TimeSpan.FromSeconds(15);
    });
}
builder.Services.AddScoped<ConfirmacaoTelefoneService>();

builder.Services.AddScoped<IImportacaoRepository, ImportacaoRepository>();
builder.Services.AddScoped<ImportacaoService>();
builder.Services.AddHostedService<ProcessadorImportacoes>();

// Eventos da outbox: o ProcessadorOutbox lê a tabela a cada 3 s e entrega ao IPublicadorEventos.
// Com Kafka:Servidores nos Segredos do Usuário: publica no Kafka (perg.pedido, perg.rota, perg.roteirizacao)
// e o ConsumidorKafka lê de volta e chama os consumidores. Sem Kafka: PublicadorInterno chama os consumidores direto.
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<PublicadorInterno>();
bool kafkaLigado = ConfiguracaoKafka.Ligado(builder.Configuration);
if (kafkaLigado)
{
    builder.Services.AddSingleton<IProducer<string, string>>(_ =>
        new ProducerBuilder<string, string>(ConfiguracaoKafka.Produtor(builder.Configuration)).Build());
    builder.Services.AddScoped<IPublicadorEventos, PublicadorKafka>();
    builder.Services.AddHostedService<ConsumidorKafka>();
}
else
{
    builder.Services.AddScoped<IPublicadorEventos>(sp => sp.GetRequiredService<PublicadorInterno>());
}
builder.Services.AddScoped<IConsumidorEvento, ConsumidorEmailCliente>();
builder.Services.AddHostedService<ProcessadorOutbox>();

builder.Services.AddScoped<IIntegracaoRepository, IntegracaoRepository>();
builder.Services.AddScoped<IntegracaoService>();
builder.Services.AddScoped<IConsumidorEvento, ConsumidorWebhooks>();
builder.Services.AddHostedService<ProcessadorWebhooks>();
builder.Services.AddHttpClient("webhooks", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("Perg-Webhooks/1.0");
});

// Geocodificação: "Nominatim" (grátis, OpenStreetMap) ou "Google" (chave em Google:GeocodingApiKey).
// Troca em Geocodificacao:Provedor no appsettings.json.
builder.Services.AddMemoryCache();
if (builder.Configuration["Geocodificacao:Provedor"] == "Google")
{
    builder.Services.AddHttpClient<IGeocodificador, GoogleGeocodificador>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(5);
    });
}
else
{
    builder.Services.AddHttpClient<IGeocodificador, NominatimGeocodificador>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
        // Exigência do Nominatim: identificar o app
        client.DefaultRequestHeaders.UserAgent.ParseAdd("PergApi/1.0 (+https://github.com/PERG-Gestao-de-Logistica)");
    });
}

builder.Services.AddScoped<IOrigemRepository, OrigemRepository>();
builder.Services.AddScoped<OrigemService>();

builder.Services.AddScoped<IPedidoRepository, PedidoRepository>();
builder.Services.AddScoped<PedidoService>();
builder.Services.AddScoped<RastreioService>();

// Roteirização: OSRM (distâncias pelas ruas) + OR-Tools (melhor ordem das entregas)
builder.Services.AddHttpClient<IMatrizDistancias, OsrmMatrizDistancias>(client =>
{
    client.Timeout = TimeSpan.FromSeconds(15);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("PergApi/1.0 (+https://github.com/PERG-Gestao-de-Logistica)");
});
builder.Services.AddScoped<OtimizadorRotas>();
builder.Services.AddScoped<IRoteirizacaoRepository, RoteirizacaoRepository>();
builder.Services.AddScoped<RoteirizacaoService>();
builder.Services.AddScoped<IRotaRepository, RotaRepository>();
builder.Services.AddScoped<RotaService>();

builder.Services.AddScoped<IPasswordHasher<Entregador>, PasswordHasher<Entregador>>();
builder.Services.AddScoped<IEntregadorRepository, EntregadorRepository>();
builder.Services.AddScoped<EntregadorService>();
builder.Services.AddScoped<IOfertaRepository, OfertaRepository>();
builder.Services.AddScoped<OfertaService>();
builder.Services.AddHostedService<ProcessadorOfertas>();

builder.Services.AddScoped<IExecucaoRepository, ExecucaoRepository>();
builder.Services.AddScoped<ExecucaoService>();
builder.Services.AddScoped<IOcorrenciaRepository, OcorrenciaRepository>();
builder.Services.AddScoped<OcorrenciaService>();
builder.Services.AddScoped<IPainelRepository, PainelRepository>();
builder.Services.AddScoped<PainelService>();

builder.Services.AddScoped<INotificacaoRepository, NotificacaoRepository>();
builder.Services.AddScoped<NotificacaoService>();

// Tempo real: SignalR (/hubs/rastreamento). A posição atual dos entregadores fica no Redis (ou na memória, sem Redis)
builder.Services.AddSingleton<NotificadorTempoReal>();
builder.Services.AddScoped<IPosicaoRepository, PosicaoRepository>();
builder.Services.AddScoped<PosicaoService>();

// Arquivos numa pasta local; em produção, Blob Storage.
builder.Services.AddSingleton<IArmazenamentoArquivos, ArmazenamentoLocal>();
builder.Services.AddScoped<IArquivoRepository, ArquivoRepository>();
builder.Services.AddScoped<ArquivoService>();

// E-mail: SMTP (Gmail com senha de app). Sem Email:Usuario nos Segredos do Usuário, só escreve no Output.
if (string.IsNullOrWhiteSpace(builder.Configuration["Email:Usuario"]))
{
    builder.Services.AddSingleton<IEnvioEmail, EmailConsole>();
}
else
{
    builder.Services.AddSingleton<IEnvioEmail, SmtpEnvioEmail>();
}

// Saque via Pix (Asaas). Sem Asaas:ApiKey nos Segredos do Usuário, usa um gateway simulado.
string? chaveAsaas = builder.Configuration["Asaas:ApiKey"];
void ConfigurarAsaas(HttpClient client)
{
    client.BaseAddress = new Uri(builder.Configuration["Asaas:BaseUrl"]!.TrimEnd('/') + "/");
    client.Timeout = TimeSpan.FromSeconds(20);
    client.DefaultRequestHeaders.Add("access_token", chaveAsaas);
    client.DefaultRequestHeaders.UserAgent.ParseAdd("PergApi/1.0");
}
if (string.IsNullOrWhiteSpace(chaveAsaas))
{
    builder.Services.AddScoped<IGatewayPagamento, GatewaySimulado>();
}
else
{
    builder.Services.AddHttpClient<IGatewayPagamento, AsaasGateway>(ConfigurarAsaas);
}
builder.Services.AddScoped<ISaqueRepository, SaqueRepository>();
builder.Services.AddScoped<SaqueService>();

// Plano e assinatura da loja: cobrança mensal no Asaas (ou simulada, sem Asaas:ApiKey)
if (string.IsNullOrWhiteSpace(chaveAsaas))
{
    builder.Services.AddScoped<ICobrancaAssinatura, CobrancaAssinaturaSimulada>();
}
else
{
    builder.Services.AddHttpClient<ICobrancaAssinatura, AsaasCobrancaAssinatura>(ConfigurarAsaas);
}
builder.Services.AddScoped<IAssinaturaRepository, AssinaturaRepository>();
builder.Services.AddScoped<LimitesPlano>();
builder.Services.AddScoped<AssinaturaService>();

// O sandbox do Asaas não conclui Pix: a simulação marca o saque como pago depois de alguns segundos.
// Só liga com o sandbox (BaseUrl com "sandbox"), nunca com o Asaas de produção.
bool asaasSandbox = builder.Configuration["Asaas:BaseUrl"]?.Contains("sandbox", StringComparison.OrdinalIgnoreCase) == true;
if (!string.IsNullOrWhiteSpace(chaveAsaas) && asaasSandbox && builder.Configuration.GetValue("Asaas:SimularConclusao", true))
{
    builder.Services.AddHostedService<ConclusaoSaquesSandbox>();
}

// Redis (ConnectionStrings:Redis nos Segredos do Usuário): códigos de verificação, posição atual dos entregadores,
// cache da checagem de acesso e backplane do SignalR. Sem a connection string, tudo fica na memória do servidor.
string? conexaoRedis = builder.Configuration.GetConnectionString("Redis");
if (string.IsNullOrWhiteSpace(conexaoRedis))
{
    builder.Services.AddSignalR();
    builder.Services.AddSingleton<IPosicaoAtualCache, PosicaoAtualCache>();
    builder.Services.AddSingleton<ICodigosVerificacao, CodigosVerificacaoMemoria>();
    builder.Services.AddSingleton<ICacheCurto, CacheCurtoMemoria>();
}
else
{
    ConfigurationOptions opcoesRedis = ConfigurationOptions.Parse(conexaoRedis);
    opcoesRedis.AbortOnConnectFail = false; // a API sobe mesmo se o Redis estiver fora; reconecta sozinha

    builder.Services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(opcoesRedis));
    builder.Services.AddSignalR().AddStackExchangeRedis(conexaoRedis, opcoes =>
    {
        opcoes.Configuration.AbortOnConnectFail = false;
        opcoes.Configuration.ChannelPrefix = RedisChannel.Literal("perg");
    });
    builder.Services.AddSingleton<IPosicaoAtualCache, PosicaoAtualRedis>();
    builder.Services.AddSingleton<ICodigosVerificacao, CodigosVerificacaoRedis>();
    builder.Services.AddSingleton<ICacheCurto, CacheCurtoRedis>();
}
builder.Services.AddScoped<VerificadorAcesso>();

builder.Services.AddScoped<IPasswordHasher<Administrador>, PasswordHasher<Administrador>>();
builder.Services.AddScoped<IBackofficeRepository, BackofficeRepository>();
builder.Services.AddScoped<BackofficeService>();

builder.Services.AddScoped<IAvaliacaoRepository, AvaliacaoRepository>();
builder.Services.AddScoped<AvaliacaoService>();

// Endereços do front (Front:Origens). Em produção, vem da variável de ambiente Front__Origens__0
string[] origensFront = builder.Configuration.GetSection("Front:Origens").Get<string[]>() ?? ["http://localhost:3000"];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Front", policy =>
        policy.WithOrigins(origensFront)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); // exigido pelo SignalR
});

// Limite de tentativas: login, senha e códigos, 10 por minuto por IP
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (contexto, _) =>
        await contexto.HttpContext.Response.WriteAsJsonAsync(new { mensagem = "Muitas tentativas. Aguarde alguns minutos e tente de novo." });

    options.AddPolicy(LimitesTentativas.Acesso, contexto => RateLimitPartition.GetFixedWindowLimiter(
        contexto.Connection.RemoteIpAddress?.ToString() ?? "desconhecido",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1) }));
});

var jwtKey = builder.Configuration["Jwt:Key"]!;
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    // "Perg" decide por requisição: com X-Api-Key → chave de API (integração); senão → token JWT
    options.DefaultScheme = "Perg";
    options.DefaultChallengeScheme = "Perg";
})
.AddPolicyScheme("Perg", "JWT ou chave de API", options =>
{
    options.ForwardDefaultSelector = contexto =>
        contexto.Request.Headers.ContainsKey(ChavesApi.Cabecalho) ? AutenticacaoChaveApi.Esquema : JwtBearerDefaults.AuthenticationScheme;
})
.AddScheme<AuthenticationSchemeOptions, AutenticacaoChaveApi>(AutenticacaoChaveApi.Esquema, null)
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
    };

    // O SignalR (WebSocket) não manda o cabeçalho Authorization: o token vem em ?access_token=
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = contexto =>
        {
            string? token = contexto.Request.Query["access_token"];
            if (!string.IsNullOrEmpty(token) && contexto.HttpContext.Request.Path.StartsWithSegments("/hubs"))
            {
                contexto.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization(options =>
{
    // Lojista = login de usuário da loja. A chave de API também tem empresa_id, mas só vale nas rotas /api/integracao/v1
    options.AddPolicy(PoliticasAcesso.Lojista, policy => policy
        .RequireClaim(GeradorTokenJwt.ClaimEmpresaId)
        .RequireAssertion(contexto => !contexto.User.HasClaim(c => c.Type == AutenticacaoChaveApi.ClaimChaveApiId)));
    options.AddPolicy(PoliticasAcesso.Entregador, policy => policy.RequireClaim(GeradorTokenJwt.ClaimEntregadorId));
    options.AddPolicy(PoliticasAcesso.Backoffice, policy => policy.RequireClaim(GeradorTokenJwt.ClaimAdministradorId));
    options.AddPolicy(PoliticasAcesso.Integracao, policy => policy.RequireClaim(AutenticacaoChaveApi.ClaimChaveApiId));
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Cole apenas o token JWT (sem a palavra Bearer)",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });

    options.AddSecurityDefinition("ChaveApi", new OpenApiSecurityScheme
    {
        Description = "Chave de API da integração (perg_...). Só para as rotas /api/integracao/v1.",
        Name = "X-Api-Key",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("ChaveApi", document)] = []
    });
});

var app = builder.Build();

// Erro não tratado: responde no mesmo formato { mensagem } (o detalhe vai para o log, não para o cliente).
// Registro repetido barrado pelo banco (dois envios no mesmo instante) vira 409.
app.UseExceptionHandler(tratamento => tratamento.Run(async contexto =>
{
    Exception? erro = contexto.Features.Get<IExceptionHandlerFeature>()?.Error;
    bool duplicado = erro is DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } };

    contexto.Response.StatusCode = duplicado ? StatusCodes.Status409Conflict : StatusCodes.Status500InternalServerError;
    await contexto.Response.WriteAsJsonAsync(new
    {
        mensagem = duplicado
            ? "Esse registro já existe ou foi enviado ao mesmo tempo por outra requisição. Confira e tente de novo."
            : "Erro inesperado no servidor. Tente de novo em instantes."
    });
}));

using (IServiceScope escopo = app.Services.CreateScope())
{
    escopo.ServiceProvider.GetRequiredService<IAssinaturaRepository>().GarantirPlanos(PlanosPadrao.Criar());

    // Primeiro administrador do painel (Backoffice:EmailInicial e SenhaInicial nos Segredos do Usuário)
    escopo.ServiceProvider.GetRequiredService<BackofficeService>().GarantirAdministradorInicial(
        builder.Configuration["Backoffice:EmailInicial"], builder.Configuration["Backoffice:SenhaInicial"]);
}

if (string.IsNullOrWhiteSpace(conexaoRedis))
{
    app.Logger.LogWarning("Redis DESLIGADO (sem ConnectionStrings:Redis): códigos e posições ficam na memória.");
}
else
{
    IConnectionMultiplexer redis = app.Services.GetRequiredService<IConnectionMultiplexer>();
    app.Logger.LogWarning(redis.IsConnected
        ? "Redis LIGADO e conectado."
        : "Redis configurado, mas SEM conexão (rede bloqueando a porta ou senha errada). Veja os erros acima.");
}

if (!kafkaLigado)
{
    app.Logger.LogWarning("Kafka DESLIGADO (sem Kafka:Servidores): eventos entregues pelo publicador interno.");
}
else
{
    // Confere a conexão e se os tópicos existem (no Aiven grátis, criar pelo Console)
    try
    {
        using IAdminClient admin = new AdminClientBuilder(ConfiguracaoKafka.Cliente(builder.Configuration)).Build();
        Metadata metadados = admin.GetMetadata(TimeSpan.FromSeconds(10));
        List<string> faltando = ConfiguracaoKafka.NomesTopicos(builder.Configuration)
            .Where(t => !metadados.Topics.Any(m => m.Topic == t && m.Error.Code == ErrorCode.NoError))
            .ToList();
        app.Logger.LogWarning(faltando.Count == 0
            ? "Kafka LIGADO e conectado."
            : "Kafka conectado, mas faltam os tópicos: " + string.Join(", ", faltando));
    }
    catch (KafkaException ex)
    {
        app.Logger.LogWarning("Kafka configurado, mas SEM conexão ({Erro}). Confira o servidor e os caminhos dos certificados.", ex.Error.Reason);
    }
    catch (InvalidOperationException ex)
    {
        app.Logger.LogWarning("Kafka configurado, mas com erro na configuração: {Erro}", ex.Message);
    }
}

// Swagger: sempre em desenvolvimento; no servidor de demonstração, com Swagger__Habilitado=true
if (app.Environment.IsDevelopment() || builder.Configuration.GetValue<bool>("Swagger:Habilitado"))
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (app.Environment.IsDevelopment())
{
    // Página de teste do tempo real: /dev/tempo-real.html
    app.UseStaticFiles();

    // No servidor, o HTTPS fica no proxy (Caddy) na frente da API
    app.UseHttpsRedirection();
}

app.UseCors("Front");

app.UseAuthentication();

// Passa a empresa do usuário logado para o filtro do banco
app.Use(async (context, next) =>
{
    string? empresaId = context.User.FindFirst(GeradorTokenJwt.ClaimEmpresaId)?.Value;
    if (Guid.TryParse(empresaId, out Guid id))
    {
        context.RequestServices.GetRequiredService<PergDbContext>().EmpresaIdAtual = id;
    }

    // Token válido não basta: quem foi desativado (ou teve a empresa suspensa) perde o acesso na hora
    var acesso = context.RequestServices.GetRequiredService<VerificadorAcesso>();
    string? usuarioId = context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
    string? entregadorId = context.User.FindFirst(GeradorTokenJwt.ClaimEntregadorId)?.Value;
    string? administradorId = context.User.FindFirst(GeradorTokenJwt.ClaimAdministradorId)?.Value;

    // Entregador em análise: só envia documentos e vê o próprio perfil até ser aprovado
    bool EntregadorLiberado(Guid id)
    {
        StatusEntregador? status = acesso.StatusDoEntregador(id);
        if (status == StatusEntregador.Ativo) return true;

        string caminho = context.Request.Path.Value ?? string.Empty;
        return status == StatusEntregador.EmAnalise
            && (caminho.StartsWith("/api/app/documentos", StringComparison.OrdinalIgnoreCase)
                || caminho.StartsWith("/api/app/entregadores/eu", StringComparison.OrdinalIgnoreCase));
    }

    bool bloqueado =
        (empresaId != null && Guid.TryParse(usuarioId, out Guid idUsuario) && !acesso.UsuarioPodeAcessar(idUsuario))
        || (Guid.TryParse(entregadorId, out Guid idEntregador) && !EntregadorLiberado(idEntregador))
        || (Guid.TryParse(administradorId, out Guid idAdministrador) && !acesso.AdministradorPodeAcessar(idAdministrador));

    if (bloqueado)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { mensagem = "Acesso desativado. Faça login de novo ou fale com o administrador." });
        return;
    }

    await next();
});

app.UseRateLimiter();

app.UseAuthorization();

app.MapControllers();
app.MapHub<RastreamentoHub>("/hubs/rastreamento");

app.Run();
