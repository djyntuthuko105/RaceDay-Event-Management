# Both applications use the same SDK stage, but have separate runtime targets.
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY RaceDay-Event-Management.API/RaceDay-Event-Management.API.csproj RaceDay-Event-Management.API/
COPY RaceDay-Event-Management.Web/RaceDay-Event-Management.Web.csproj RaceDay-Event-Management.Web/
RUN dotnet restore RaceDay-Event-Management.API && dotnet restore RaceDay-Event-Management.Web
COPY RaceDay-Event-Management.API/ RaceDay-Event-Management.API/
COPY RaceDay-Event-Management.Web/ RaceDay-Event-Management.Web/
RUN dotnet publish RaceDay-Event-Management.API -c Release --no-restore -o /out/api /p:UseAppHost=false \
    && dotnet publish RaceDay-Event-Management.Web -c Release --no-restore -o /out/web /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS api
WORKDIR /app
COPY --from=build --chown=app:app /out/api .
RUN mkdir -p /app/data /app/wwwroot/event-images && chown -R app:app /app/data /app/wwwroot/event-images
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDay-Event-Management.API.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS web
WORKDIR /app
COPY --from=build --chown=app:app /out/web .
USER app
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "RaceDay-Event-Management.Web.dll"]
