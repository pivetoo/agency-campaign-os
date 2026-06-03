# Deploy do Kanvas (pull-based, sem conexao de entrada do GitHub)

A borda de rede da Hostinger (anti-DDoS, acima da VM) dropa de forma intermitente os
IPs dos runners do GitHub em **qualquer porta** (22 SSH e 443 HTTPS). Por isso o deploy
NAO pode depender de o GitHub conectar no VPS. O modelo e **pull**: o proprio VPS puxa
as imagens novas do GHCR (sentido VPS -> internet sempre funciona).

## Como funciona

1. `deploy.yml` builda e publica as imagens no GHCR (`build-api` / `build-web`).
2. O job `deploy` so faz um `curl` **best-effort** ao webhook (deploy instantaneo quando
   o IP do runner nao esta bloqueado). Esse passo NUNCA falha o run.
3. **Garantia:** um timer systemd no VPS (`kanvas-deploy-poll.timer`, a cada 30s) roda
   `deploy.sh`, que faz `docker compose pull` + `up -d` e aplica qualquer imagem nova.
   Resultado: deploy garantido em ate ~30s, mesmo com o webhook bloqueado.

`deploy.sh` usa `flock` para serializar webhook + poll, e so faz `prune` quando algo mudou.

## Arquivos

- `receiver.py` — webhook HTTP (so `127.0.0.1:9876`), publicado pelo nginx em `/deploy-hook`.
  Autentica por token, grava o compose recebido e chama `deploy.sh`. -> `/opt/kanvas-deploy/`
- `deploy.sh` — pull + up -d + prune (com lock). Fonte unica usada pelo webhook E pelo poll.
  -> `/opt/kanvas-deploy/deploy.sh`
- `kanvas-deploy-hook.service` — systemd do receiver do webhook. -> `/etc/systemd/system/`
- `kanvas-deploy-poll.service` + `kanvas-deploy-poll.timer` — o poll (garantia). -> `/etc/systemd/system/`
- `nginx-location.conf` — `location /deploy-hook` no vhost `kanvas.mainstay.com.br`.

## Setup no VPS (uma vez)

```bash
mkdir -p /opt/kanvas-deploy
# copiar receiver.py e deploy.sh para /opt/kanvas-deploy/ (chmod 0755)
openssl rand -hex 48 > /opt/kanvas-deploy/token   # NUNCA commitar; chmod 600
# copiar os 3 units para /etc/systemd/system/
systemctl daemon-reload
systemctl enable --now kanvas-deploy-hook.service     # webhook (instantaneo, best-effort)
systemctl enable --now kanvas-deploy-poll.timer       # poll (garantia, 30s)
# nginx: adicionar nginx-location.conf no server{} 443, depois: nginx -t && systemctl reload nginx
```

Secret do GitHub (so para o webhook best-effort):

```bash
ssh root@VPS 'cat /opt/kanvas-deploy/token' | gh secret set DEPLOY_HOOK_TOKEN -R pivetoo/agency-campaign-os
```

## Observacoes

- O repo e privado, entao o VPS nao busca o `docker-compose.prod.yml` do GitHub. O compose
  vive em `/var/www/kanvas/docker-compose.prod.yml`; o webhook o sincroniza quando alcancavel.
  Se MUDAR o compose enquanto o webhook estiver bloqueado, atualizar o arquivo no VPS via SSH.
- Credenciais do GHCR ficam em `/root/.docker/config.json` (o `deploy.sh` roda como root).
- O poll faz `docker compose pull` a cada 30s (checagem de manifesto leve); so baixa camadas
  e recria containers quando a imagem realmente muda.
