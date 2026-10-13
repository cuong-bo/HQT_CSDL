FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY api/Hue_travel.Api.csproj api/
RUN dotnet restore api/Hue_travel.Api.csproj
COPY api api
RUN dotnet publish api/Hue_travel.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Hue_travel.Api.dll"]
