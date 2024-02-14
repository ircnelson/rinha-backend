FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["src/Rinha2024q1.WebApplication/Rinha2024q1.WebApplication.csproj", "src/Rinha2024q1.WebApplication/"]
RUN dotnet restore "src/Rinha2024q1.WebApplication/Rinha2024q1.WebApplication.csproj"
COPY . .
WORKDIR "/src/src/Rinha2024q1.WebApplication"
RUN dotnet build "Rinha2024q1.WebApplication.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "Rinha2024q1.WebApplication.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Rinha2024q1.WebApplication.dll"]
