# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0.400-alpine3.24 AS build
WORKDIR /src

COPY global.json ./
COPY services/catalog/src/Leds.Catalog.Api/Leds.Catalog.Api.csproj services/catalog/src/Leds.Catalog.Api/
COPY services/catalog/src/Leds.Catalog.Application/Leds.Catalog.Application.csproj services/catalog/src/Leds.Catalog.Application/
COPY services/catalog/src/Leds.Catalog.Domain/Leds.Catalog.Domain.csproj services/catalog/src/Leds.Catalog.Domain/
COPY services/catalog/src/Leds.Catalog.Infrastructure/Leds.Catalog.Infrastructure.csproj services/catalog/src/Leds.Catalog.Infrastructure/
RUN dotnet restore services/catalog/src/Leds.Catalog.Api/Leds.Catalog.Api.csproj

COPY services/catalog/src/ services/catalog/src/
RUN dotnet publish services/catalog/src/Leds.Catalog.Api/Leds.Catalog.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /app/publish \
    -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0.11-alpine3.24 AS runtime
WORKDIR /app
RUN apk add --no-cache curl
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_EnableDiagnostics=0
EXPOSE 8080
COPY --from=build --chown=$APP_UID:$APP_UID /app/publish ./
USER $APP_UID
ENTRYPOINT ["dotnet", "Leds.Catalog.Api.dll"]
