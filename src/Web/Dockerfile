# Образ Web (Blazor-клієнт). Контекст збірки — корінь репозиторію (див. docker-compose.yml).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY CrossApp.sln Directory.Build.props ./
COPY src/Contracts/*.csproj src/Contracts/
COPY src/Web/*.csproj src/Web/
RUN dotnet restore src/Web/Web.csproj

COPY . .
RUN dotnet publish src/Web/Web.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080
ENTRYPOINT ["dotnet", "Web.dll"]
