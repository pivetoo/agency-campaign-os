# Releases (versionamento semantico)

O deploy e continuo (todo push na `main` -> imagens `:latest` que o VPS puxa). Um **release**
e um marco deliberado por cima do que ja esta em producao: serve de changelog, numero de
versao do produto e ponto de rollback. Releases NAO sao automaticos por commit (a `main`
recebe commits demais, e release deve ser uma decisao).

## Como cortar um release

1. GitHub -> **Actions** -> **Release Kanvas** -> **Run workflow**.
2. Escolha o incremento:
   - `patch` -> correcoes (0.1.0 -> 0.1.1)
   - `minor` -> novas funcionalidades (0.1.0 -> 0.2.0)
   - `major` -> mudanca incompativel (0.x -> 1.0.0)
3. O workflow calcula a versao a partir da ultima tag, re-taga a imagem **ja buildada** desse
   commit como `:vX.Y.Z` (sem rebuild) e cria a tag + GitHub Release com notas automaticas.

Pre-requisito: o commit do `HEAD` ja precisa ter sido buildado pelo deploy (todo push na
`main` builda). Se acabou de empurrar, espere o run de **Deploy Kanvas** desse commit terminar.

## Convencao de numeracao

- Pre-lancamento: andar em **0.x** (`0.1.0`, `0.2.0`, ...). O primeiro release deve ser `minor`
  (a partir de 0.0.0 da `0.1.0`).
- No go-live do SaaS Mainstay: cravar **1.0.0** (escolher `major` quando estiver em 0.x e quiser ir pra 1.0.0... ou criar a tag `v1.0.0` na mao).

## Rollback para uma versao

As imagens versionadas ficam no GHCR (`:vX.Y.Z`), entao da pra fixar uma versao:

```bash
# no VPS, em /var/www/kanvas/docker-compose.prod.yml, trocar :latest por :v0.1.0 nas imagens,
# depois deixar o poll aplicar (ate ~30s) ou rodar na mao:
docker compose -f docker-compose.prod.yml up -d
```

Para voltar ao continuo, retornar as imagens para `:latest`.

## O que e o release (resumo)

- **Tag git** `vX.Y.Z` -> aponta pro commit.
- **GitHub Release** -> changelog automatico desde a tag anterior.
- **Imagem** `ghcr.io/pivetoo/agency-campaign-{api,web}:vX.Y.Z` -> artefato imutavel para rollback.
