# <img width="50px" height="40px" alt="icon" src="https://github.com/user-attachments/assets/6ad63636-804e-49f2-b1b7-dc414d72e35f" /> WebWrapperNet


A simple wrapper to turn web applications into desktop applications using WebView.

## How to use
1. Open the powershell in your `index.html` folder and run: <pre>Invoke-WebRequest -Uri "https://github.com/JoaoVKS/WebWrapperNet/releases/download/v1.0.0/WebWrapperNet_EXE.zip" -OutFile "temp.zip"; Expand-Archive -Path "temp.zip" -DestinationPath "."; Remove-Item "temp.zip"</pre>

Or

1. Publish the project (e.g.: `dotnet publish -c Release -r win-x64 --self-contained`) — the `.exe` will be generated at `bin\Release\net10.0-windows\win-x64\publish\WebWrap\`
2. Place the generated `.exe` in the same folder as your `index.html`
3. Run the `.exe`

Or

1. Download the `.zip` of the latest release
2. Extract the contents where your `index.html` is located
3. Run the extracted `.exe`

See the folder SampleProject for an example of how to use WebWrapperNet in a project.

WebWrapper will:
- Open a window with the `index.html` content using the `file://` protocol
- Automatically load the page title and icon
- Expose an HTTP proxy for requests

## Requirements

- .NET 10 — https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- WebView2 Runtime — https://developer.microsoft.com/en-us/microsoft-edge/webview2/

---

# <img width="50px" height="40px" alt="icon" src="https://github.com/user-attachments/assets/6ad63636-804e-49f2-b1b7-dc414d72e35f" /> WebWrapperNet

Um wrapper simples para transformar aplicações web em aplicações desktop usando WebView.

## Como usar
1. Abra o powershell na pasta do seu `index.html` e execute o comando: <pre>Invoke-WebRequest -Uri "https://github.com/JoaoVKS/WebWrapperNet/releases/download/v1.0.0/WebWrapperNet_EXE.zip" -OutFile "temp.zip"; Expand-Archive -Path "temp.zip" -DestinationPath "."; Remove-Item "temp.zip"</pre>

Ou

1. Publique o projeto (ex.: `dotnet publish -c Release -r win-x64 --self-contained`) —> o `.exe` será gerado em `bin\Release\net10.0-windows\win-x64\publish\WebWrap\`
2. Coloque o `.exe` gerado na mesma pasta do seu `index.html`
3. Execute o `.exe`

Ou

1. Baixe o `.zip` da última release
2. Extraia o conteúdo na pasta onde está o `index.html`
3. Execute o `.exe` extraído

Veja a pasta SampleProject para um exemplo de como usar o WebWrapperNet em um projeto.

O WebWrapper irá:
- Abrir uma janela com o conteúdo do `index.html` usando o protocolo `file://`
- Carregar automaticamente o título e ícone da página
- Disponibilizar um proxy para requisições HTTP

## Requisitos

- .NET 10 — https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- WebView2 Runtime — https://developer.microsoft.com/en-us/microsoft-edge/webview2/
