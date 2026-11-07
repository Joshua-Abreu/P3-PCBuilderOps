# Etapa 1: compila y publica la API con el SDK de .NET 10.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilacion
WORKDIR /src

COPY src/API/API.csproj src/API/
COPY src/Core/Core.csproj src/Core/
COPY src/Modules/PCBuilderOps/PCBuilderOps.csproj src/Modules/PCBuilderOps/
RUN dotnet restore src/API/API.csproj

COPY src/ src/
RUN dotnet publish src/API/API.csproj -c Release -o /app/publicado --no-restore

# Etapa 2: solo el runtime de ASP.NET Core y lo publicado. Sin secretos: llegan como variables desde .env.
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS ejecucion
WORKDIR /app

# RF-DOC-05: carpeta del volumen de documentos, escribible por el usuario sin privilegios de la imagen.
RUN mkdir -p /app/documentos && chown "$APP_UID" /app/documentos

COPY --from=compilacion /app/publicado .

ENV ASPNETCORE_HTTP_PORTS=8080 \
    DOCUMENTOS_RUTA=/app/documentos

USER $APP_UID
EXPOSE 8080
ENTRYPOINT ["dotnet", "API.dll"]
