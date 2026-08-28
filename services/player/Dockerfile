# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0.400-alpine3.24 AS build
WORKDIR /src

COPY global.json ./
COPY services/player/src/Leds.Player.Api/Leds.Player.Api.csproj services/player/src/Leds.Player.Api/
COPY services/player/src/Leds.Player.Application/Leds.Player.Application.csproj services/player/src/Leds.Player.Application/
COPY services/player/src/Leds.Player.Domain/Leds.Player.Domain.csproj services/player/src/Leds.Player.Domain/
COPY services/player/src/Leds.Player.Infrastructure/Leds.Player.Infrastructure.csproj services/player/src/Leds.Player.Infrastructure/
RUN dotnet restore services/player/src/Leds.Player.Api/Leds.Player.Api.csproj

COPY services/player/src/ services/player/src/
RUN dotnet publish services/player/src/Leds.Player.Api/Leds.Player.Api.csproj \
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
ENTRYPOINT ["dotnet", "Leds.Player.Api.dll"]
