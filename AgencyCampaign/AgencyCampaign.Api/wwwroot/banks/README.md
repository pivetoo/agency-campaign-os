# Logos de bancos

Esta pasta hospeda os logos dos bancos seedados pelo sistema, servidos como assets estáticos pelo `UseStaticFiles` em `Program.cs`.

## Convenção

- Cada arquivo segue o padrão `{compe}.svg`, onde `compe` é o código de 3 dígitos do banco (ex.: `001.svg` = BB, `237.svg` = Bradesco, `341.svg` = Itaú).
- SVG (vetorial) é o formato padrão — escala sem perder qualidade em qualquer tamanho.

## URL pública

Os arquivos ficam acessíveis em `${VITE_API_BASE_URL}/banks/{compe}.svg`. A migration `202605180012_SwitchBankLogosToSvg` aponta esse path em `logourl`, e o frontend resolve via `resolveUploadUrl` (lib/uploadUrl.ts).

Se o arquivo não existir, o frontend renderiza fallback com o código compe em fundo cinza.

## Origem dos arquivos

Logos vetoriais baixados de [Tgentil/Bancos-em-SVG](https://github.com/Tgentil/Bancos-em-SVG). 28 dos 30 bancos seedados foram encontrados. Não disponíveis no repo de origem:

- `376` — JPMorgan
- `477` — Citibank

Para esses dois, o frontend mostra fallback com o código compe. Se precisar, adicione manualmente um `376.svg` e `477.svg` aqui.

## Sobre direitos de marca

Os logos pertencem aos respectivos bancos (marca registrada). O uso em ERP de gestão financeira para identificar contas do próprio cliente configura uso nominativo (Lei 9.279/96, art. 132 II) — prática estabelecida em ferramentas similares de mercado.

## Como adicionar ou substituir um logo

1. Coloque o arquivo aqui com o nome correto (`{compe}.svg`).
2. Faça commit normalmente.
3. No próximo deploy, o asset fica disponível automaticamente.

Para bancos custom criados por agências, o logo é gerenciado via upload pela tela de Configuração de Bancos (armazenado em `/uploads/banks/{tenant}/{id}.webp` pelo `IImageUploadStorage`).
