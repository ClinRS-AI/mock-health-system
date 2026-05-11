#!/bin/sh
# Render (and similar PaaS) set PORT; ASP.NET Core must listen on all interfaces.
set -e
export ASPNETCORE_URLS="http://0.0.0.0:${PORT:-8080}"
exec dotnet MockHealthSystem.Api.dll
