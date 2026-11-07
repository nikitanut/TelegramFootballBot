# Moving the bot to a new server

The deploy account is `footballbot`. The GitHub Actions deploy key stays the same. Production runs from `/opt/footballbot`, dev from `/opt/footballbot-dev`. Each `data` directory holds `BotDb.db`, the bot token, the Google credentials, and the log.

## 1. Prepare the new server

SSH in as root:

```bash
useradd --create-home --shell /bin/bash footballbot
apt-get update && apt-get install -y git
git clone https://github.com/nikitanut/TelegramFootballBot.git /home/TelegramFootballBot
bash /home/TelegramFootballBot/deploy/bootstrap.sh footballbot
```

`bootstrap.sh` installs Docker, lets `footballbot` run it, installs the deploy public key, and creates both `/opt` directories.

## 2. Stop the bot on the old server

```bash
cd /opt/footballbot && docker compose stop
cd /opt/footballbot-dev && docker compose stop
```

Leave both stopped. Two running copies split Telegram updates between them.

## 3. Copy the data

From your own computer:

```bash
scp -r root@OLD_SERVER:/opt/footballbot/data root@NEW_SERVER:/opt/footballbot/
scp -r root@OLD_SERVER:/opt/footballbot-dev/data root@NEW_SERVER:/opt/footballbot-dev/
```

Copying as root is fine. The container gives those files to `footballbot` when it starts.

## 4. Point GitHub at the new server

In both the Production and Development environments, set `REMOTE_HOST` to the new address. Leave the rest as they are:

| Setting | Production | Development |
|---|---|---|
| `REMOTE_USER` | `footballbot` | `footballbot` |
| `TARGET` | `/opt/footballbot` | `/opt/footballbot-dev` |
| `SSH_PRIVATE_KEY` | unchanged | unchanged |

## 5. Start it

Re-run the last successful workflow for a `development/…` branch, then the last one for `master`. Check dev before production:

```bash
cd /opt/footballbot-dev
docker compose ps
docker compose logs -f
```

Production uses the same commands in `/opt/footballbot`.

## Day to day

Restart:

```bash
cd /opt/footballbot-dev
docker compose restart
```

Logs:

```bash
docker compose logs -f
```

A code change goes on a `development/…` branch. That push deploys only the dev bot. Merge to `master` after dev looks right. That deploys production.

`distributionTime` and `gameDay` live in `appsettings.json` in the repository. Every deploy writes that file again, so a time changed only on the server is replaced on the next deploy.
