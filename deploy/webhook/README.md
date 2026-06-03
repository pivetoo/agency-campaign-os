# Deploy via webhook HTTPS

O deploy do Kanvas NAO usa SSH de entrada. A borda de rede da Hostinger (anti-DDoS,
acima da VM) dropa de forma intermitente os IPs dos runners do GitHub na porta 22,
enquanto o trafego web (443) passa normal. Por isso o `deploy.yml` apenas faz um
`curl` HTTPS para um receiver que roda no proprio VPS.

## Fluxo

1. `deploy.yml` builda e publica as imagens no GHCR (jobs `build-api`/`build-web`).
2. O job `deploy` faz `POST https://kanvas.mainstay.com.br/deploy-hook` com o header
   `X-Deploy-Token: <DEPLOY_HOOK_TOKEN>` e o `docker-compose.prod.yml` no corpo.
3. O nginx do host roteia `/deploy-hook` para `127.0.0.1:9876` (`receiver.py`).
4. O receiver autentica o token, grava o compose recebido e roda `deploy.sh`
   (`docker compose pull` + `up -d --remove-orphans` + `prune`).

## Arquivos

- `receiver.py` — servidor HTTP (so escuta em `127.0.0.1:9876`). Valida token
  constant-time, valida o compose (`services:`), grava e dispara o deploy. Lock
  contra execucao concorrente. -> `/opt/kanvas-deploy/receiver.py`
- `deploy.sh` — pull + up -d + prune. -> `/opt/kanvas-deploy/deploy.sh`
- `kanvas-deploy-hook.service` — unit systemd que mantem o receiver no ar.
  -> `/etc/systemd/system/`
- `nginx-location.conf` — `location` a adicionar no vhost `kanvas.mainstay.com.br`.

## Setup no VPS (uma vez)

```bash
mkdir -p /opt/kanvas-deploy
# copiar receiver.py e deploy.sh para /opt/kanvas-deploy/ (chmod 0755)
openssl rand -hex 48 > /opt/kanvas-deploy/token   # NUNCA commitar; chmod 600
# copiar kanvas-deploy-hook.service para /etc/systemd/system/
systemctl daemon-reload && systemctl enable --now kanvas-deploy-hook.service
# adicionar nginx-location.conf no server{} 443 do vhost, depois: nginx -t && systemctl reload nginx
```

Registrar o MESMO token como secret `DEPLOY_HOOK_TOKEN` no GitHub:

```bash
ssh root@VPS 'cat /opt/kanvas-deploy/token' | gh secret set DEPLOY_HOOK_TOKEN -R pivetoo/agency-campaign-os
```

O token vive apenas no VPS (`/opt/kanvas-deploy/token`) e no secret do GitHub.
Para rotacionar, gerar novo no VPS e atualizar o secret. As credenciais do GHCR
ficam em `/root/.docker/config.json` (o `deploy.sh` roda como root e as reaproveita).
