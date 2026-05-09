FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy project files
COPY ["src/Auth.Api/Auth.Api.csproj", "src/Auth.Api/"]
COPY ["src/Auth.Application/Auth.Application.csproj", "src/Auth.Application/"]
COPY ["src/Auth.Domain/Auth.Domain.csproj", "src/Auth.Domain/"]
COPY ["src/Auth.Infrastructure/Auth.Infrastructure.csproj", "src/Auth.Infrastructure/"]
COPY ["src/Auth.Persistence/Auth.Persistence.csproj", "src/Auth.Persistence/"]
COPY ["Directory.Build.props", "."]

# Restore packages
RUN dotnet restore "src/Auth.Api/Auth.Api.csproj"

# Copy all source code
COPY . .

# Build and publish
WORKDIR "/src/src/Auth.Api"
RUN dotnet build "Auth.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Auth.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

# Create keys directory (will be overridden by volume mount in production)
RUN mkdir -p /app/keys

ENTRYPOINT ["dotnet", "Auth.Api.dll"]
