# Agent notes

Server moves are documented in `deploy/MOVING.md`.

## Deploy

- A push to `development/…` deploys the dev bot only. A push or merge to `master` deploys production.
- Dev uses the Development GitHub environment, image tag `dev`, and `/opt/footballbot-dev`. Production uses the Production environment, image tag `latest`, and `/opt/footballbot`.
- Do not deploy by merging to `master` to see if a change works. Verify the dev bot first.
- Write config with `deploy/substitute-config.py`. Do not use `devops-actions/variable-substitution`; that action fails to download.
- Run `deploy/bootstrap.sh` with `bash`. It is not marked executable.

## Schedule

`distributionTime` and `gameDay` are `TimeSpan` values: `{weekday}.{HH:mm:ss}`. Weekday is Monday = 1 through Sunday = 7. The clock is Moscow time, a fixed UTC+3 offset. Example: `6.14:17:00` is Saturday 14:17.

Both values live in `appsettings.json` in the repo. Every deploy rewrites the server file from that copy, so an edit made only on the server is lost on the next deploy. The scheduler checks once a minute and matches the hour and minute exactly.

## Data

The live database is `BotDb.db` in the server `data` directory. Stop the container before copying it. Do not run two copies of the same bot; Telegram delivers each update to only one of them.

The container runs as `footballbot` and takes ownership of `/data` on startup. A database copied as root is writable after the next start.
