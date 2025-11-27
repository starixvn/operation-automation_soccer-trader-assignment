# Stage 1: Build
FROM hub.nexdev.net/base/dotnet/core/sdk:9.0-alpine AS builder
WORKDIR /src

# Copy csproj files and restore dependencies
COPY ["src/SoccerLST.TraderAssignment.API/SoccerLST.TraderAssignment.API.csproj", "src/SoccerLST.TraderAssignment.API/"]
COPY ["src/SoccerLST.TraderAssignment.Core/SoccerLST.TraderAssignment.Core.csproj", "src/SoccerLST.TraderAssignment.Core/"]
RUN dotnet restore "src/SoccerLST.TraderAssignment.API/SoccerLST.TraderAssignment.API.csproj"

# Copy the rest of the source code
COPY src/ src/

# Build the application
WORKDIR /src/src/SoccerLST.TraderAssignment.API
RUN dotnet build "SoccerLST.TraderAssignment.API.csproj" -c Release -o /app/build

# Stage 2: Publish
FROM build AS publish
RUN dotnet publish "SoccerLST.TraderAssignment.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 3: Runtime
FROM hub.nexdev.net/base/dotnet/core/aspnet:9.0-alpine AS base

WORKDIR /app

# Install dependencies for Google OR-Tools
RUN apk add --no-cache \
    curl \
    libgomp

# Copy published application
COPY --from=publish /app/publish .

# Expose port
EXPOSE 5000
EXPOSE 5001

# Set environment variables
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production

# Run the application
ENTRYPOINT ["dotnet", "SoccerLST.TraderAssignment.API.dll"]
