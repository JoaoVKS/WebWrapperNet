# <img width="50px" height="40px" alt="icon" src="https://github.com/user-attachments/assets/6ad63636-804e-49f2-b1b7-dc414d72e35f" /> WebWrapperNet


A simple wrapper to turn web applications into desktop applications using WebView.
Exposes WebMessages to allow communication between the web page and the wrapper, allowing you to execute code in the wrapper using JavaScript 🚀.

Used in Pwsh.Ai https://github.com/JoaoVKS/Pwsh.ai

## How to use
1. Open the powershell in your `index.html` folder and run: <pre>Invoke-WebRequest -Uri "https://github.com/JoaoVKS/WebWrapperNet/releases/download/V1.0.2/WepWrapperNet_Required.zip" -OutFile "temp.zip"; Expand-Archive -Path "temp.zip" -DestinationPath "."; Remove-Item "temp.zip"</pre>
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
- Expose web messages to handle PowerShell manipulation and other interactions.

### WebMessages API
- **httpRequest** - Make HTTP requests
- **pwshNew** - Create a new PowerShell process
- **pwshInput** - Send input to PowerShell
- **pwshKill** - Kill a PowerShell process
- **pwshStop** - Stop a PowerShell process
- **pwshAsyncOutput** - Get async output from PowerShell
- **brwsReload** - Reload the browser
- **fileWrite** - Write content to a file
- **fileRead** - Read content from a file
- **fileTextSearch** - Search text in a file
- **sysInfo** - Get system information
- **rawToMd** - Convert documents (PDF, Word, HTML, JSON, etc.) to Markdown

### SampleHttpRequest:
Sample of how to use the WebWrapperNet WebMessages to make requests using JavaScript
- Download: <pre>Invoke-WebRequest -Uri "https://github.com/JoaoVKS/WebWrapperNet/releases/download/V1.0.2/WebWrapper_SampleHttp.zip" -OutFile "temp.zip"; Expand-Archive -Path "temp.zip" -DestinationPath "."; Remove-Item "temp.zip"</pre>

## Requirements

- .NET 10 — https://dotnet.microsoft.com/en-us/download/dotnet/10.0
- WebView2 Runtime — https://developer.microsoft.com/en-us/microsoft-edge/webview2/