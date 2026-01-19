# syntax=docker/dockerfile:1

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Observability_WebAPI_Blazor.Shared/Observability_WebAPI_Blazor.Shared.csproj Observability_WebAPI_Blazor.Shared/
COPY Observability_WebAPI_Blazor/Observability_WebAPI_Blazor.Client/Observability_WebAPI_Blazor.Client.csproj Observability_WebAPI_Blazor/Observability_WebAPI_Blazor.Client/
COPY Observability_WebAPI_Blazor/Observability_WebAPI_Blazor/Observability_WebAPI_Blazor.csproj Observability_WebAPI_Blazor/Observability_WebAPI_Blazor/
RUN dotnet restore Observability_WebAPI_Blazor/Observability_WebAPI_Blazor/Observability_WebAPI_Blazor.csproj

COPY . .
WORKDIR /src/Observability_WebAPI_Blazor/Observability_WebAPI_Blazor
RUN dotnet publish -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
ENTRYPOINT ["dotnet", "Observability_WebAPI_Blazor.dll"]