FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY TelegramFootballBot.App/TelegramFootballBot.App.csproj TelegramFootballBot.App/
RUN dotnet restore TelegramFootballBot.App/TelegramFootballBot.App.csproj
COPY TelegramFootballBot.App/ TelegramFootballBot.App/
RUN dotnet publish TelegramFootballBot.App/TelegramFootballBot.App.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/runtime:10.0
RUN apt-get update \
    && apt-get install -y --no-install-recommends util-linux \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app .
COPY deploy/entrypoint.sh /entrypoint.sh
RUN chmod +x /entrypoint.sh
ENV DATA_DIR=/data
ENTRYPOINT ["/entrypoint.sh"]
