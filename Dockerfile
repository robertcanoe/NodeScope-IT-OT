# ============================================================
# Stage 1: Build and publish the application
# ============================================================
FROM mcr.microsoft.com/dotnet/sdk:9.0-bookworm-slim AS build
ARG BUILD_CONFIGURATION=Release

WORKDIR /src

COPY ["src/NodeScope.Api/NodeScope.Api.csproj", "src/NodeScope.Api/"]
COPY ["src/NodeScope.Application/NodeScope.Application.csproj", "src/NodeScope.Application/"]
COPY ["src/NodeScope.Domain/NodeScope.Domain.csproj", "src/NodeScope.Domain/"]
COPY ["src/NodeScope.Infrastructure/NodeScope.Infrastructure.csproj", "src/NodeScope.Infrastructure/"]
COPY ["src/NodeScope.ServiceDefaults/NodeScope.ServiceDefaults.csproj", "src/NodeScope.ServiceDefaults/"]

RUN dotnet restore "src/NodeScope.Api/NodeScope.Api.csproj"

COPY . .

WORKDIR "/src/src/NodeScope.Api"
RUN dotnet publish "NodeScope.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# ============================================================
# Stage 2: Final runtime image
# ============================================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS final

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl ca-certificates \
    && rm -rf /var/lib/apt/lists/*

WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080

EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

USER 65532

ENTRYPOINT ["dotnet", "NodeScope.Api.dll"]
