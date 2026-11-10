using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Perg.Api.Applications.Arquivos;
using Perg.Api.Applications.Autenticacao;
using Perg.Api.Applications.Email;
using Perg.Api.Applications.Geocodificacao;
using Perg.Api.Applications.Importacoes;
using Perg.Api.Applications.Ofertas;
using Perg.Api.Applications.Pagamentos;
using Perg.Api.Applications.Roteamento;
using Perg.Api.Applications.Services;
using Perg.Api.Applications.TempoReal;
using Perg.Api.Contexts;
using Perg.Api.Domains;
using Perg.Api.Hubs;
using Perg.Api.Interfaces;
using Perg.Api.Repositories;

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

builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<UsuarioService>();
builder.Services.AddScoped<EquipeService>();
builder.Services.AddScoped<RecuperacaoSenhaService>();

// Importação de pedidos por planilha (.xlsx/.csv), processada em segundo plano
builder.Services.AddScoped<IImportacaoRepository, ImportacaoRepository>();
builder.Services.AddScoped<ImportacaoService>();
builder.Services.AddHostedService<ProcessadorImportacoes>();

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

// Entregadores e ofertas de rota
builder.Services.AddScoped<IPasswordHasher<Entregador>, PasswordHasher<Entregador>>();
builder.Services.AddScoped<IEntregadorRepository, EntregadorRepository>();
builder.Services.AddScoped<EntregadorService>();
builder.Services.AddScoped<IOfertaRepository, OfertaRepository>();
builder.Services.AddScoped<OfertaService>();
builder.Services.AddHostedService<ProcessadorOfertas>();

// Execução da rota (app) e ocorrências (loja)
builder.Services.AddScoped<IExecucaoRepository, ExecucaoRepository>();
builder.Services.AddScoped<ExecucaoService>();
builder.Services.AddScoped<IOcorrenciaRepository, OcorrenciaRepository>();
builder.Services.AddScoped<OcorrenciaService>();

// Sino de notificações da loja
builder.Services.AddScoped<INotificacaoRepository, NotificacaoRepository>();
builder.Services.AddScoped<NotificacaoService>();

// Tempo real: SignalR (/hubs/rastreamento) + posição atual dos entregadores em memória
builder.Services.AddSignalR();
builder.Services.AddSingleton<PosicaoAtualCache>();
builder.Services.AddSingleton<NotificadorTempoReal>();
builder.Services.AddScoped<IPosicaoRepository, PosicaoRepository>();
builder.Services.AddScoped<PosicaoService>();

// Arquivos (fotos, comprovantes, documentos). Hoje numa pasta local; depois, Blob Storage.
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

// Códigos de verificação (esqueci a senha). Em memória por enquanto; Redis depois.
builder.Services.AddSingleton<ICodigosVerificacao, CodigosVerificacaoMemoria>();

// Saque via Pix (Asaas). Sem Asaas:ApiKey nos Segredos do Usuário, usa um gateway simulado.
string? chaveAsaas = builder.Configuration["Asaas:ApiKey"];
if (string.IsNullOrWhiteSpace(chaveAsaas))
{
    builder.Services.AddScoped<IGatewayPagamento, GatewaySimulado>();
}
else
{
    builder.Services.AddHttpClient<IGatewayPagamento, AsaasGateway>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Asaas:BaseUrl"]!.TrimEnd('/') + "/");
        client.Timeout = TimeSpan.FromSeconds(20);
        client.DefaultRequestHeaders.Add("access_token", chaveAsaas);
        client.DefaultRequestHeaders.UserAgent.ParseAdd("PergApi/1.0");
    });
}
builder.Services.AddScoped<ISaqueRepository, SaqueRepository>();
builder.Services.AddScoped<SaqueService>();

// O sandbox do Asaas não conclui Pix: a simulação marca o saque como pago depois de alguns segundos.
// Só liga com o sandbox (BaseUrl com "sandbox"), nunca com o Asaas de produção.
bool asaasSandbox = builder.Configuration["Asaas:BaseUrl"]?.Contains("sandbox", StringComparison.OrdinalIgnoreCase) == true;
if (!string.IsNullOrWhiteSpace(chaveAsaas) && asaasSandbox && builder.Configuration.GetValue("Asaas:SimularConclusao", true))
{
    builder.Services.AddHostedService<ConclusaoSaquesSandbox>();
}

// Front em Next.js rodando local
builder.Services.AddCors(options =>
{
    options.AddPolicy("Front", policy =>
        policy.WithOrigins("http://localhost:3000")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials()); // exigido pelo SignalR
});

var jwtKey = builder.Configuration["Jwt:Key"]!;
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
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

// Quem é loja não acessa o app do entregador e vice-versa
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(PoliticasAcesso.Lojista, policy => policy.RequireClaim(GeradorTokenJwt.ClaimEmpresaId));
    options.AddPolicy(PoliticasAcesso.Entregador, policy => policy.RequireClaim(GeradorTokenJwt.ClaimEntregadorId));
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
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    // Página de teste do tempo real: /dev/tempo-real.html
    app.UseStaticFiles();
}

app.UseHttpsRedirection();

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
    await next();
});

app.UseAuthorization();

app.MapControllers();
app.MapHub<RastreamentoHub>("/hubs/rastreamento");

app.Run();
