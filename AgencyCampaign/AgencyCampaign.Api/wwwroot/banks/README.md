# Logos de bancos

Esta pasta hospeda os logos dos bancos seedados pelo sistema, servidos como assets estáticos pelo `UseStaticFiles` em `Program.cs`.

## Convenção

- Cada arquivo segue o padrão `{compe}.png`, onde `compe` é o código de 3 dígitos do banco (ex.: `001.png` = BB, `237.png` = Bradesco, `341.png` = Itaú).
- PNG é o formato padrão (vindo do open-source `react-native-brazil-bank-icons`, MIT).
- Substituir por SVG é OK se preferir — basta atualizar `logourl` no banco para `.svg`.

## URL pública

Os arquivos ficam acessíveis em `${VITE_API_BASE_URL}/banks/{compe}.png`. A migration `202605180010_SeedBankLogoPaths` aponta esse path em `logourl`, e o frontend resolve via `resolveUploadUrl` (lib/uploadUrl.ts).

Se o arquivo não existir, o frontend renderiza fallback com a inicial do banco em fundo cinza.

## Origem dos arquivos

Logos baixados de [DuduLourenco/react-native-brazil-bank-icons](https://github.com/DuduLourenco/react-native-brazil-bank-icons) (MIT). 29 dos 30 bancos seedados foram encontrados; **003 (Banco da Amazônia)** ficou sem logo — fallback de inicial cobre o caso. Adicione manualmente se precisar.

## Como adicionar / substituir um logo

1. Coloque o arquivo aqui com o nome correto (`{compe}.png` ou `.svg`).
2. Se mudar a extensão, atualize `logourl` na tabela `bank` (pela tela de Configuração ou via SQL).
3. Faça commit normalmente.

Para bancos custom criados por agências, o campo `logoUrl` aceita URL externa direta — esta pasta é só para os bancos seedados como `IsSystem = true`.
