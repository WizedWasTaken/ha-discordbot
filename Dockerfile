# Use official .NET 8.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Use .NET 8.0 SDK to build the bot
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY BotCore/BotTemplate.csproj ./BotCore/
WORKDIR /src/BotCore
RUN dotnet restore

# Copy everything else and build the bot
COPY . .  
RUN dotnet publish -c Release -o /app/publish

# Create final image with runtime only
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

# Set Discord token using environment variable
ENV DISCORD_TOKEN=${DISCORD_TOKEN}

# Run the bot
ENTRYPOINT ["dotnet", "BotCore.dll"]
