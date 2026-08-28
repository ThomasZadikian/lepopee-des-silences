# syntax=docker/dockerfile:1.7
FROM mcr.microsoft.com/dotnet/sdk:10.0.400-alpine3.24 AS build
WORKDIR /src

COPY global.json ./
COPY packages/shared-building-blocks/src/Leds.SharedBuildingBlocks/Leds.SharedBuildingBlocks.csproj packages/shared-building-blocks/src/Leds.SharedBuildingBlocks/
COPY services/game-engine/src/Leds.GameEngine.Api/Leds.GameEngine.Api.csproj services/game-engine/src/Leds.GameEngine.Api/
COPY services/game-engine/src/Leds.GameEngine.Application/Leds.GameEngine.Application.csproj services/game-engine/src/Leds.GameEngine.Application/
COPY services/game-engine/src/Leds.GameEngine.Domain/Leds.GameEngine.Domain.csproj services/game-engine/src/Leds.GameEngine.Domain/
COPY services/game-engine/src/Leds.GameEngine.Infrastructure/Leds.GameEngine.Infrastructure.csproj services/game-engine/src/Leds.GameEngine.Infrastructure/
RUN dotnet restore services/game-engine/src/Leds.GameEngine.Api/Leds.GameEngine.Api.csproj

COPY packages/shared-building-blocks/src/Leds.SharedBuildingBlocks/ packages/shared-building-blocks/src/Leds.SharedBuildingBlocks/
COPY services/game-engine/src/ services/game-engine/src/
RUN dotnet publish services/game-engine/src/Leds.GameEngine.Api/Leds.GameEngine.Api.csproj \
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
ENTRYPOINT ["dotnet", "Leds.GameEngine.Api.dll"]
