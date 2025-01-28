# Use official .NET 8.0 SDK image for build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the project files and restore dependencies
COPY BotTemplate.csproj ./
RUN dotnet restore

# Copy the entire project and build the application
COPY . ./
RUN dotnet publish -c Release -o /out

# Use .NET 8.0 runtime for final image
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Copy built files from build stage
COPY --from=build /out ./

# Expose necessary ports (if applicable)
# EXPOSE 5000  (for ASP.NET Core apps)
# EXPOSE 80

# Set the entrypoint
CMD ["dotnet", "BotTemplate.dll"]
