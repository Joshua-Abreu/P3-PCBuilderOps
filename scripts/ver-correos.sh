#!/bin/bash
# Muestra los últimos correos de la cola con su enlace (activación o recuperación).
# Uso, desde la carpeta del proyecto: docker compose exec db bash /scripts/ver-correos.sh [correo]
filtro=""
if [ -n "$1" ]; then
  filtro="WHERE Destinatario = N'${1//\'/\'\'}'"
fi

/opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -d PCBuilderOps -W -s " | " -Q "SET NOCOUNT ON;
SELECT TOP 10 Destinatario, Asunto, Estado, SUBSTRING(Cuerpo, CHARINDEX('http', Cuerpo), 1000) AS Enlace
FROM CorreosEnCola $filtro
ORDER BY FechaCreacionUtc DESC"
