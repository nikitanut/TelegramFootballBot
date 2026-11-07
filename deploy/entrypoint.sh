#!/bin/sh
set -eu

if [ -n "${BOT_UID:-}" ] && [ -n "${BOT_GID:-}" ]; then
  mkdir -p /data
  chown -R "${BOT_UID}:${BOT_GID}" /data
  exec setpriv --reuid="${BOT_UID}" --regid="${BOT_GID}" --clear-groups --inh-caps=-all \
    dotnet TelegramFootballBot.App.dll
fi

exec dotnet TelegramFootballBot.App.dll
