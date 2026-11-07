FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TelegramFootballBot.App/TelegramFootballBot.App.csproj TelegramFootballBot.App/
RUN dotnet restore TelegramFootballBot.App/TelegramFootballBot.App.csproj
COPY TelegramFootballBot.App/ TelegramFootballBot.App/
RUN dotnet publish TelegramFootballBot.App/TelegramFootballBot.App.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
ENV DATA_DIR=/data
ENTRYPOINT ["dotnet", "TelegramFootballBot.App.dll"]
