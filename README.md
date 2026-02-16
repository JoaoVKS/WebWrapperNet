WebWrapperNet
A simple wrapper to transform web applications into desktop applications using WebView.

How to Use
Publish the project (e.g., dotnet publish -c Release -r win-x64 --self-contained) —> the .exe will be generated in bin\Release\net10.0-windows\win-x64\publish\WebWrap\

Place the generated .exe in the same folder as your index.html file.

Run the .exe.

WebWrapper will:

Open a window displaying your index.html content using the file:// protocol.

Automatically load the page's title and icon.

Provide a proxy for HTTP requests.

Requirements
.NET 10 — Download .NET 10

WebView2 Runtime — Download WebView2
---
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
