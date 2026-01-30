# -------------------
# Base runtime image
# -------------------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

# -------------------
# Build image
# -------------------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy csproj and restore dependencies
COPY WebApplication1/WebApplication1.csproj ./WebApplication1/
WORKDIR /src/WebApplication1
RUN dotnet restore "WebApplication1.csproj"

# Copy everything else
COPY WebApplication1/. ./
# Build the project
RUN dotnet build "WebApplication1.csproj" -c Release -o /app/build

# -------------------
# Publish
# -------------------
FROM build AS publish
RUN dotnet publish "WebApplication1.csproj" -c Release -o /app/publish

# -------------------
# Final runtime image
# -------------------
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish ./

# Entry point
ENTRYPOINT ["dotnet", "WebApplication1.dll"]
