FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/Hue_travel.Api/Hue_travel.Api.csproj src/Hue_travel.Api/
RUN dotnet restore src/Hue_travel.Api/Hue_travel.Api.csproj
COPY src/Hue_travel.Api src/Hue_travel.Api
RUN dotnet publish src/Hue_travel.Api/Hue_travel.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
USER $APP_UID
ENTRYPOINT ["dotnet", "Hue_travel.Api.dll"]
