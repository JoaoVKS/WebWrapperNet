# WebWrapperNet

Um wrapper simples para transformar aplicações web em aplicações desktop usando WebView.

## Como usar

1. Compile o projeto (o `.exe` será gerado em `bin\Debug\net10.0-windows\` ou `bin\Release\net10.0-windows\`)
2. Coloque o `.exe` gerado na mesma pasta do seu `index.html`
3. Execute o `.exe`

O WebWrapper irá:
- Abrir uma janela com o conteúdo do `index.html` usando o protocolo `file://`
- Carregar automaticamente o título e ícone da página
- Disponibilizar um proxy para requisições HTTP

## Requisitos

- .NET 10
