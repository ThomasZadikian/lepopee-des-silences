FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copier uniquement les fichiers .csproj
COPY RPG_ESI07.API/RPG_ESI07.API.csproj              RPG_ESI07.API/
COPY RPG_ESI07.Application/RPG_ESI07.Application.csproj    RPG_ESI07.Application/
COPY RPG_ESI07.Domain/RPG_ESI07.Domain.csproj         RPG_ESI07.Domain/
COPY RPG_ESI07.Infrastructure/RPG_ESI07.Infrastructure.csproj RPG_ESI07.Infrastructure/

# Restore sans la solution
RUN dotnet restore RPG_ESI07.API/RPG_ESI07.API.csproj

# Copier tout le code
COPY . .

RUN dotnet publish RPG_ESI07.API/RPG_ESI07.API.csproj \
    -c Release \
    -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
RUN mkdir -p logs
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "RPG_ESI07.API.dll"]