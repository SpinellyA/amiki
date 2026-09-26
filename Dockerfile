# Builds the API and the Blazor client together; the API serves both.

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restore first, from project files only, so code changes don't re-download packages.
COPY Amiki.sln ./
COPY Amiki.Core/Amiki.Core.csproj Amiki.Core/
COPY Amiki.Client/Amiki.Client.csproj Amiki.Client/
COPY Amiki.Api/Amiki.Api.csproj Amiki.Api/
RUN dotnet restore Amiki.Api/Amiki.Api.csproj

COPY . .
RUN dotnet publish Amiki.Api/Amiki.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .

# Listen on 8080 (set PORT=8080 on Render so it routes there). The host terminates HTTPS in
# front of the app; forwarded headers let the app know requests really arrived over HTTPS,
# which Google sign-in needs to build the right https:// callback address.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true
EXPOSE 8080

USER $APP_UID
ENTRYPOINT ["dotnet", "Amiki.Api.dll"]
