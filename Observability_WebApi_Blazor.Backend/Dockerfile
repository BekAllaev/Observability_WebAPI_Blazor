# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Observability_WebApi_Blazor.Backend/Observability_WebApi_Blazor.Backend.csproj Observability_WebApi_Blazor.Backend/
RUN dotnet restore Observability_WebApi_Blazor.Backend/Observability_WebApi_Blazor.Backend.csproj

COPY . .
WORKDIR /src/Observability_WebApi_Blazor.Backend
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Observability_WebApi_Blazor.Backend.dll"]