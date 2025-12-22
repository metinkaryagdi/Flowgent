# =========================
# BUILD STAGE
# =========================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Build context: BitirmeProject root
# Tüm solution'ý içeri kopyala
COPY . .

# IdentityService.Api projesine geç
WORKDIR /src/src/services/identity/IdentityService.Api

RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# =========================
# RUNTIME STAGE
# =========================
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "IdentityService.Api.dll"]
