# ─── Build Stage ──────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copy project files
COPY PasswordVault.Domain/PasswordVault.Domain.csproj           PasswordVault.Domain/
COPY PasswordVault.Application/PasswordVault.Application.csproj PasswordVault.Application/
COPY PasswordVault.Infrastructure/PasswordVault.Infrastructure.csproj PasswordVault.Infrastructure/
COPY PasswordVault.API/PasswordVault.API.csproj                 PasswordVault.API/

# Restore
RUN dotnet restore PasswordVault.API/PasswordVault.API.csproj

# Copy all source
COPY . .

# Publish
RUN dotnet publish PasswordVault.API/PasswordVault.API.csproj \
    -c Release -o /app/publish --no-restore

# ─── Runtime Stage ─────────────────────────────────────────────────────────────
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
EXPOSE 5000

COPY --from=build /app/publish .

# Create logs directory and set permissions before switching to non-root user
RUN mkdir -p logs && chown -R $APP_UID /app

# Use the built-in non-root user provided by .NET runtime images
USER $APP_UID

ENTRYPOINT ["dotnet", "PasswordVault.API.dll"]
