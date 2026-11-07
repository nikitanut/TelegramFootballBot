#!/bin/bash
# Prepare a new VPS for the football bot. Safe to run again.
# Usage: sudo ./deploy/bootstrap.sh <ssh-user>
set -euo pipefail

if [ "$(id -u)" -ne 0 ]; then
  echo "Run as root: sudo ./deploy/bootstrap.sh <ssh-user>"
  exit 1
fi

DEPLOY_USER="${1:-${SUDO_USER:-}}"
if [ -z "$DEPLOY_USER" ] || [ "$DEPLOY_USER" = "root" ]; then
  echo "Pass the account GitHub Actions will SSH as: sudo ./deploy/bootstrap.sh myuser"
  exit 1
fi

if ! id "$DEPLOY_USER" >/dev/null 2>&1; then
  echo "User $DEPLOY_USER does not exist."
  exit 1
fi

export DEBIAN_FRONTEND=noninteractive
apt-get update
apt-get install -y ca-certificates curl
if ! command -v docker >/dev/null 2>&1; then
  curl -fsSL https://get.docker.com | sh
fi
systemctl enable --now docker
usermod -aG docker "$DEPLOY_USER"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
PUB="$ROOT/deploy/github-actions.pub"
HOME_DIR="$(getent passwd "$DEPLOY_USER" | cut -d: -f6)"
install -d -m 700 -o "$DEPLOY_USER" -g "$DEPLOY_USER" "$HOME_DIR/.ssh"
touch "$HOME_DIR/.ssh/authorized_keys"
if ! grep -qxF -f "$PUB" "$HOME_DIR/.ssh/authorized_keys"; then
  cat "$PUB" >> "$HOME_DIR/.ssh/authorized_keys"
fi
chown "$DEPLOY_USER:$DEPLOY_USER" "$HOME_DIR/.ssh/authorized_keys"
chmod 600 "$HOME_DIR/.ssh/authorized_keys"

UID_NUM="$(id -u "$DEPLOY_USER")"
GID_NUM="$(id -g "$DEPLOY_USER")"

install_instance() {
  local dir="$1"
  local tag="$2"
  install -d -o "$DEPLOY_USER" -g "$DEPLOY_USER" "$dir/data"
  install -m 644 -o "$DEPLOY_USER" -g "$DEPLOY_USER" "$ROOT/docker-compose.yml" "$dir/docker-compose.yml"
  cat > "$dir/.env" <<EOF
IMAGE_TAG=$tag
BOT_UID=$UID_NUM
BOT_GID=$GID_NUM
EOF
  chown "$DEPLOY_USER:$DEPLOY_USER" "$dir/.env"
  chmod 600 "$dir/.env"
  chown -R "$DEPLOY_USER:$DEPLOY_USER" "$dir"
}

install_instance /opt/footballbot latest
install_instance /opt/footballbot-dev dev

echo "Docker is ready."
echo "Production:  /opt/footballbot       (image tag latest)"
echo "Development: /opt/footballbot-dev   (image tag dev)"
echo "Copy BotDb.db into the data directory as $DEPLOY_USER. If the copy is owned by root, run this script again."
