# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution-level files and restore as distinct layers for caching
COPY ["JobHandling.API/JobHandling.API.csproj", "JobHandling.API/"]
COPY ["JobHandling.Application/JobHandling.Application.csproj", "JobHandling.Application/"]
COPY ["JobHandling.Domain/JobHandling.Domain.csproj", "JobHandling.Domain/"]
COPY ["JobHandling.Infrastructure/JobHandling.Infrastructure.csproj", "JobHandling.Infrastructure/"]
RUN dotnet restore "JobHandling.API/JobHandling.API.csproj"

# Copy everything else and build
COPY . .
WORKDIR "/src/JobHandling.API"
RUN dotnet publish -c Release -o /app/publish --no-restore

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Run as non-root user for security
RUN adduser --disabled-password --gecos "" appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

ENTRYPOINT ["dotnet", "JobHandling.API.dll"]