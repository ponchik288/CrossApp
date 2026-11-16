# Образ Api. Збирається з кореня репозиторію: docker compose build (або docker build -t crossapp-api .)

# ---- етап 1: збірка. Образ SDK містить компілятор; у фінальний образ він не потрапляє ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Спершу лише файли проєктів: шар з dotnet restore кешується, доки не зміняться залежності.
COPY CrossApp.sln Directory.Build.props ./
COPY src/Core/*.csproj src/Core/
COPY src/Infrastructure/*.csproj src/Infrastructure/
COPY src/Contracts/*.csproj src/Contracts/
COPY src/Api/*.csproj src/Api/
RUN dotnet restore src/Api/Api.csproj

# Тепер решта коду. Зміна .cs-файлу перезбирає лише шари нижче цього рядка.
COPY . .
RUN dotnet publish src/Api/Api.csproj -c Release -o /app/publish --no-restore

# ---- етап 2: фінальний образ. Лише runtime ASP.NET Core і результат publish ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
RUN mkdir -p /app/data
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "Api.dll"]
