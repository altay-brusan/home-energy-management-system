# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution file and project files
COPY ["Hems-Backend.sln", "./"]
COPY ["src/Domain/Domain.csproj", "src/Domain/"]
COPY ["src/Application/Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Web/Web.csproj", "src/Web/"]

# Copy Directory.Build.props and Directory.Packages.props for centralized package management
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]

# Restore dependencies
RUN dotnet restore "src/Web/Web.csproj"

# Copy the entire source code
COPY . .

# Build the application
WORKDIR /src/src/Web
RUN dotnet build "Web.csproj" -c Release -o /app/build

# Publish the application
FROM build AS publish
RUN dotnet publish "Web.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

# Create non-root user for security
RUN adduser --disabled-password --gecos "" --uid 1000 appuser && chown -R appuser /app
USER appuser

# Copy published application from build stage
COPY --from=publish /app/publish .

# Expose port (ASP.NET Core default non-root port)
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production

# Health check
HEALTHCHECK --interval=30s --timeout=3s --start-period=5s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "Hems_Backend.Web.dll"]
