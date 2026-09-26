#!/bin/sh
set -e

PORT="${PORT:-8080}"
export ASPNETCORE_URLS="http://0.0.0.0:${PORT}"

echo "Iniciando Plataforma de Incidencias en puerto ${PORT}..."
exec dotnet PlataformaIncidencias.dll
