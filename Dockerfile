# Use official .NET 8.0 runtime as base image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app

# Use .NET 8.0 SDK to build the bot
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy the project file and restore dependencies
COPY BotTemplate.csproj ./
RUN dotnet restore

# Copy everything else and build the bot
COPY . .
RUN dotnet publish -c Release -o /app/publish

# Create final image with runtime only
FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

ENV DISCORD_TOKEN="MTMzMzMwMDU3NDA1MzA3NzA0Mg.G6U9y8.7dfeOA4Z4lIAd9fMtk-qWyHsIu6TLtd7FMnNwk"

# Run the bot
ENTRYPOINT ["dotnet", "BotTemplate.dll"]
