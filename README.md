# WebWrapperNet

Um wrapper simples para transformar aplicações web em aplicações desktop usando WebView.

## Como usar

1. Publique o projeto (ex.: `dotnet publish -c Release -r win-x64 --self-contained`) —> o `.exe` será gerado em `bin\Release\net10.0-windows\win-x64\publish\WebWrap\`
2. Coloque o `.exe` gerado na mesma pasta do seu `index.html`
3. Execute o `.exe`

O WebWrapper irá:
- Abrir uma janela com o conteúdo do `index.html` usando o protocolo `file://`
- Carregar automaticamente o título e ícone da página
- Disponibilizar um proxy para requisições HTTP

## Requisitos

- .NET 10 — https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- WebView2 Runtime — https://developer.microsoft.com/en-us/microsoft-edge/webview2/
