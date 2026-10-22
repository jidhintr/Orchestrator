FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY global.json Directory.Build.props Orchestrator.slnx .editorconfig ./
COPY src/Orchestrator.Api/Orchestrator.Api.csproj src/Orchestrator.Api/
COPY src/Orchestrator.Domain/Orchestrator.Domain.csproj src/Orchestrator.Domain/
COPY src/Orchestrator.Application/Orchestrator.Application.csproj src/Orchestrator.Application/
COPY src/Orchestrator.Infrastructure/Orchestrator.Infrastructure.csproj src/Orchestrator.Infrastructure/
RUN dotnet restore src/Orchestrator.Api
COPY src src
RUN dotnet publish src/Orchestrator.Api -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Orchestrator.Api.dll"]
