# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy the project files
COPY **/*.csproj ./
RUN dotnet restore

# Copy the rest of the source code and build the application
COPY . .
RUN dotnet publish -c Release -o /out

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# Copy the build output from the previous stage
COPY --from=build /out .

# Entry point of the console app
ENTRYPOINT ["dotnet", "BotCore.dll"]
