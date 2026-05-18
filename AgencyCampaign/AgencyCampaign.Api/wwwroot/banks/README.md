# Logos de bancos

Esta pasta hospeda os logos dos bancos seedados pelo sistema, servidos como assets estáticos pelo `UseStaticFiles` em `Program.cs`.

## Convenção

- Cada arquivo segue o padrão `{compe}.svg`, onde `compe` é o código de 3 dígitos do banco (ex.: `001.svg` = BB, `237.svg` = Bradesco, `341.svg` = Itaú).
- Preferir **SVG** (escalável, leve). PNG é aceito como fallback.
- Recomendação visual: fundo transparente, contorno legível em fundos claros e escuros.

## URL pública

Os arquivos ficam acessíveis em `${VITE_API_BASE_URL}/banks/{compe}.svg`. A migration que seedou os bancos system aponta esse path em `logourl`, e o frontend resolve via `resolveUploadUrl` (lib/uploadUrl.ts).

Se o arquivo SVG não existir, o frontend renderiza fallback com a inicial do banco em fundo cinza.

## Como adicionar / substituir um logo

1. Coloque o arquivo aqui com o nome correto (`{compe}.svg`).
2. Faça commit normalmente.
3. No próximo deploy, o asset fica disponível automaticamente.

Para bancos custom criados por agências, o campo `logoUrl` aceita URL externa direta — esta pasta é só para os bancos seedados como `IsSystem = true`.
