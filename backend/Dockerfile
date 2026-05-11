# Build context: backend/
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MockHealthSystem.sln ./
COPY MockHealthSystem.Api/ MockHealthSystem.Api/
COPY MockHealthSystem.Application/ MockHealthSystem.Application/
COPY MockHealthSystem.Infrastructure/ MockHealthSystem.Infrastructure/

RUN mkdir -p \
  MockHealthSystem.Api/bin/Debug MockHealthSystem.Api/bin/Release \
  MockHealthSystem.Application/bin/Debug MockHealthSystem.Application/bin/Release \
  MockHealthSystem.Infrastructure/bin/Debug MockHealthSystem.Infrastructure/bin/Release

RUN dotnet restore MockHealthSystem.Api/MockHealthSystem.Api.csproj \
  && dotnet publish MockHealthSystem.Api/MockHealthSystem.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
COPY docker-entrypoint.sh /app/docker-entrypoint.sh
RUN chmod +x /app/docker-entrypoint.sh
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["/app/docker-entrypoint.sh"]
