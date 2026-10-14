FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY BazosWatchdog/BazosWatchdog.csproj BazosWatchdog/
RUN dotnet restore BazosWatchdog/BazosWatchdog.csproj
COPY BazosWatchdog/ BazosWatchdog/
RUN dotnet publish BazosWatchdog/BazosWatchdog.csproj --configuration Release --output /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    TZ=Europe/Prague
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "BazosWatchdog.dll"]
