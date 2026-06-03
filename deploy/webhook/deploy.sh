#!/usr/bin/env bash
set -euo pipefail
cd /var/www/kanvas
echo "[deploy] $(date -u '+%Y-%m-%d %H:%M:%S UTC') pull"
docker compose -f docker-compose.prod.yml pull
echo "[deploy] up -d"
docker compose -f docker-compose.prod.yml up -d --remove-orphans
docker image prune -f
echo "[deploy] done"
