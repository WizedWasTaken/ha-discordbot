# Use official .NET 8.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Use .NET 8.0 SDK to build the bot
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
# Restore dependencies using a separate layer
COPY ["BotTemplate.csproj", "src/"]
WORKDIR /src
RUN dotnet restore


# Copy everything else and build the bot
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Create final image with runtime only
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV DISCORD_TOKEN=${DISCORD_TOKEN}

# Run the bot
ENTRYPOINT ["dotnet", "BotTemplate.dll"]
