# Use the official .NET 8.0 SDK image for the build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Set the working directory in the container
WORKDIR /app

# Copy the solution file and project files to the container
COPY BotTemplate.sln ./
COPY BotTemplate.csproj ./

# Restore the dependencies
RUN dotnet restore ./BotTemplate.csproj

# Copy the rest of the application files
COPY . ./

# Build the application
RUN dotnet publish ./BotTemplate.csproj -c Release -o /app/publish

# Use the official .NET 8.0 runtime image for the runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base

# Set the working directory in the container
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Expose the necessary port
EXPOSE 5000

# Command to run the application
ENTRYPOINT ["dotnet", "BotTemplate.dll"]
