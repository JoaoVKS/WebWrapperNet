# <img width="50px" height="40px" alt="icon" src="https://github.com/user-attachments/assets/6ad63636-804e-49f2-b1b7-dc414d72e35f" /> WebWrapperNet


A simple wrapper to turn web applications into desktop applications using WebView.
Exposes WebMessages to allow communication between the web page and the wrapper, allowing you to execute code in the wrapper using JavaScript 🚀.

## How to use
1. Open the powershell in your `index.html` folder and run: <pre>Invoke-WebRequest -Uri "https://github.com/JoaoVKS/WebWrapperNet/releases/download/v1.0.2/WepWrapperNet_Required.zip" -OutFile "temp.zip"; Expand-Archive -Path "temp.zip" -DestinationPath "."; Remove-Item "temp.zip"</pre>
2. Run the `.exe`

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
- Start a web server to serve the `index.html` content.
- Open a window with the `index.html` content.
- Automatically load the page title and icon.
- Expose an HTTP proxy for requests.
- Expose web messages to handle PowerShell manipulation.

## Samples
### SampleHttpRequest:
Sample of how to use the WebWrapperNet WebMessages to make requests using JavaScript
- Download: <pre>Invoke-WebRequest -Uri "https://github.com/JoaoVKS/WebWrapperNet/releases/download/v1.0.2/WebWrapper_SampleHttp.zip" -OutFile "temp.zip"; Expand-Archive -Path "temp.zip" -DestinationPath "."; Remove-Item "temp.zip"</pre>
### SamplePowerShell
Sample of how to use the WebWrapperNet WebMessages to execute PowerShell commands using JavaScript
- Download: <pre>Invoke-WebRequest -Uri "https://github.com/JoaoVKS/WebWrapperNet/releases/download/v1.0.2/WebWrapper_SamplePowershell.zip" -OutFile "temp.zip"; Expand-Archive -Path "temp.zip" -DestinationPath "."; Remove-Item "temp.zip"</pre>

## Requirements

- .NET 10 — https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- WebView2 Runtime — https://developer.microsoft.com/en-us/microsoft-edge/webview2/

---

# <img width="50px" height="40px" alt="icon" src="https://github.com/user-attachments/assets/6ad63636-804e-49f2-b1b7-dc414d72e35f" /> WebWrapperNet

Um wrapper simples para transformar aplicações web em aplicações desktop usando WebView. 
Dispõe de WebMessages para permitir a comunicação entre a página web e o wrapper, permitindo que você execute código no wrapper usando JavaScript 🚀.

## Como usar
1. Abra o powershell na pasta do seu `index.html` e execute o comando: <pre>Invoke-WebRequest -Uri "https://github.com/JoaoVKS/WebWrapperNet/releases/download/v1.0.2/WepWrapperNet_Required.zip" -OutFile "temp.zip"; Expand-Archive -Path "temp.zip" -DestinationPath "."; Remove-Item "temp.zip"</pre>
2. Execute o `.exe`

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
- Iniciar um servidor web para servir o conteúdo do `index.html`.
- Abrir uma janela com o conteúdo do `index.html`.
- Carregar automaticamente o título e ícone da página.
- Disponibilizar um proxy para requisições HTTP.
- Disponibilizar web messages para manipulação do PowerShell.

## Exemplos
### SampleHttpRequest:
Exemplo de como usar as WebMessages do WebWrapperNet para fazer requisições usando JavaScript
- Download: <pre>Invoke-WebRequest -Uri "https://github.com/JoaoVKS/WebWrapperNet/releases/download/v1.0.2/WebWrapper_SampleHttp.zip" -OutFile "temp.zip"; Expand-Archive -Path "temp.zip" -DestinationPath "."; Remove-Item "temp.zip"</pre>
### SamplePowerShell
Exemplo de como usar as WebMessages do WebWrapperNet para executar comandos do PowerShell usando JavaScript
- Download: <pre>Invoke-WebRequest -Uri "https://github.com/JoaoVKS/WebWrapperNet/releases/download/v1.0.2/WebWrapper_SamplePowershell.zip" -OutFile "temp.zip"; Expand-Archive -Path "temp.zip" -DestinationPath "."; Remove-Item "temp.zip"</pre>

## Requisitos

- .NET 10 — https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- WebView2 Runtime — https://developer.microsoft.com/en-us/microsoft-edge/webview2/
