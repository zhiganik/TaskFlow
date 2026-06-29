# Stage 1 — restore (cached until .csproj files change)
FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine AS restore
WORKDIR /src

COPY Directory.Build.props .
COPY TaskFlow.Contracts/TaskFlow.Contracts.csproj TaskFlow.Contracts/
COPY TaskFlow.Api/TaskFlow.Api.csproj TaskFlow.Api/
COPY TaskFlow.Application/TaskFlow.Application.csproj TaskFlow.Application/
COPY TaskFlow.Infrastructure/TaskFlow.Infrastructure.csproj TaskFlow.Infrastructure/
RUN dotnet restore TaskFlow.Api/TaskFlow.Api.csproj

# Stage 2 — build & publish
FROM restore AS build
COPY TaskFlow.Contracts/ TaskFlow.Contracts/
COPY TaskFlow.Api/ TaskFlow.Api/
COPY TaskFlow.Application/ TaskFlow.Application/
COPY TaskFlow.Infrastructure/ TaskFlow.Infrastructure/
RUN dotnet publish TaskFlow.Api/TaskFlow.Api.csproj -c Release -o /app/publish --no-restore

# Stage 3 — runtime only (no SDK, no source)
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS runtime
WORKDIR /app

RUN apk add --no-cache curl \
 && addgroup -S appgroup && adduser -S appuser -G appgroup
COPY --from=build /app/publish .
RUN mkdir -p /app/uploads/processed /app/uploads/avatars \
 && chown -R appuser:appgroup /app
USER appuser

EXPOSE 8080
ENTRYPOINT ["dotnet", "TaskFlow.Api.dll"]
