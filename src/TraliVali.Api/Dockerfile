# Build stage - restore and build
FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src

# Copy solution and project files
COPY ["src/TraliVali.Api/TraliVali.Api.csproj", "src/TraliVali.Api/"]
COPY ["src/TraliVali.Domain/TraliVali.Domain.csproj", "src/TraliVali.Domain/"]
COPY ["src/TraliVali.Infrastructure/TraliVali.Infrastructure.csproj", "src/TraliVali.Infrastructure/"]
COPY ["src/TraliVali.Auth/TraliVali.Auth.csproj", "src/TraliVali.Auth/"]
COPY ["src/TraliVali.Messaging/TraliVali.Messaging.csproj", "src/TraliVali.Messaging/"]
COPY ["src/TraliVali.Workers/TraliVali.Workers.csproj", "src/TraliVali.Workers/"]

# Restore dependencies
RUN dotnet restore "src/TraliVali.Api/TraliVali.Api.csproj"

# Copy the rest of the source code
COPY src/ src/

# Build the application
WORKDIR "/src/src/TraliVali.Api"
RUN dotnet build "TraliVali.Api.csproj" -c Release -o /app/build

# Publish stage - create optimized release
FROM build AS publish
RUN dotnet publish "TraliVali.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage - minimal runtime image
FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS final
WORKDIR /app

# Create non-root user for security
RUN addgroup -g 1000 appuser && \
    adduser -u 1000 -G appuser -s /bin/sh -D appuser && \
    chown -R appuser:appuser /app

# Copy published application
COPY --from=publish --chown=appuser:appuser /app/publish .

# Switch to non-root user
USER appuser

# Configure ASP.NET Core to listen on port 8080 (non-privileged)
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Health check
HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD wget --no-verbose --tries=1 --spider http://localhost:8080/weatherforecast || exit 1

ENTRYPOINT ["dotnet", "TraliVali.Api.dll"]
