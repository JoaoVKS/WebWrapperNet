class WebWrapClient {
    constructor() {
        this.handlers = new Map();
        window.chrome?.webview?.addEventListener("message", (event) => {
            const message = event.data;
            if (!message?.type) {
                return;
            }
            const callbacks = this.handlers.get(message.type) || [];
            callbacks.forEach((callback) => callback(message));
        });
    }

    sendMessage(type, data = {}) {
        const payload = {
            type,
            requestId: data.requestId || crypto.randomUUID(),
            ...data
        };
        window.chrome?.webview?.postMessage(payload);
        console.log("SENT:", type, payload);
        return payload.requestId;
    }

    onMessage(type, callback) {
        const list = this.handlers.get(type) || [];
        list.push(callback);
        console.log("RECEIVED:", type, callback);
        this.handlers.set(type, list);
    }

    createPwsh(name, asyncOutput = true) {
        return this.sendMessage("pwshNew", { name, keepOpen: true, asyncOutput });
    }

    sendCommand(requestId, command) {
        return this.sendMessage("pwshInput", { requestId, command });
    }

    killPwsh(requestId) {
        return this.sendMessage("pwshKill", { requestId });
    }

    stopCommand(requestId) {
        return this.sendMessage("pwshStop", { requestId });
    }
}

window.WebWrapClient = WebWrapClient;
