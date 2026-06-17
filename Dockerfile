# ── Stage 1: Build ────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy project files first (layer caching)
COPY ["GLMS-CORE-APP/GLMS-CORE-APP.csproj",               "GLMS-CORE-APP/"]
COPY ["GLMS-CORE-APP.Shared/GLMS-CORE-APP.Shared.csproj", "GLMS-CORE-APP.Shared/"]

# Restore dependencies
RUN dotnet restore "GLMS-CORE-APP/GLMS-CORE-APP.csproj"

# Copy all source files
COPY GLMS-CORE-APP/        GLMS-CORE-APP/
COPY GLMS-CORE-APP.Shared/ GLMS-CORE-APP.Shared/

# Build and publish
WORKDIR "/src/GLMS-CORE-APP"
RUN dotnet publish "GLMS-CORE-APP.csproj" -c Release -o /app/publish

# ── Stage 2: Runtime ───────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

RUN mkdir -p /app/wwwroot/uploads/contracts

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "GLMS-CORE-APP.dll"]