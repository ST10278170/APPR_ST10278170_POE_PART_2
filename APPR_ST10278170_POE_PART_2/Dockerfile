# Use Linux-based ASP.NET Core runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Build stage with Linux-based SDK
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project file and restore dependencies
COPY ["APPR_ST10278170_POE_PART_2.csproj", "./"]
RUN dotnet restore "./APPR_ST10278170_POE_PART_2.csproj"

# Copy all files and build the project
COPY . .
RUN dotnet build "./APPR_ST10278170_POE_PART_2.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the project
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./APPR_ST10278170_POE_PART_2.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final runtime image
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "APPR_ST10278170_POE_PART_2.dll"]
