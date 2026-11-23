# Imagem da API do Perg. Build a partir da raiz do repositório: docker build -t perg-api .
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY Perg.Api/Perg.Api.csproj Perg.Api/
RUN dotnet restore Perg.Api/Perg.Api.csproj
COPY Perg.Api/ Perg.Api/
RUN dotnet publish Perg.Api/Perg.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app

# OR-Tools usa a biblioteca OpenMP do sistema; a pasta de arquivos precisa ser do usuário da API
USER root
RUN apt-get update && apt-get install -y --no-install-recommends libgomp1 && rm -rf /var/lib/apt/lists/* \
    && mkdir -p /app/armazenamento && chown $APP_UID /app/armazenamento
USER $APP_UID

COPY --from=build /app/publish .

# O HTTPS fica no Caddy; a API escuta HTTP na rede interna e lê o X-Forwarded-* do proxy
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 8080
ENTRYPOINT ["dotnet", "Perg.Api.dll"]
