function generateUUID() {
  return ([1e7]+-1e3+-4e3+-8e3+-1e11).replace(/[018]/g, c =>
    (c ^ crypto.getRandomValues(new Uint8Array(1))[0] & 15 >> c / 4).toString(16)
  );
}
function decodeUnicodeEscapes(s){
  return s.replace(/\\u([\dA-Fa-f]{4})/g, (_, h) => String.fromCharCode(parseInt(h,16)));
}
let webWrapper = {
    async ProxyFetch(url, options = {}) {
        return new Promise((resolve, reject) => {
            const requestId = generateUUID();
            const handler = (event) => {
                const data = event.data;
                if (data.requestId !== requestId) return;
                window.chrome.webview.removeEventListener('message', handler);
                if (data.type === 'httpResponse') {
                    resolve({
                        ok: data.status >= 200 && data.status < 300,
                        status: data.status,
                        statusText: data.statusText,
                        headers: data.headers,
                        text: async () => data.body,
                        json: async () => JSON.parse(data.body)
                    });
                } else if (data.type === 'httpError') {
                    data.statusText = decodeUnicodeEscapes(data.statusText);
                    reject(new Error(decodeUnicodeEscapes(data.statusText) || 'HTTP request failed'));
                    console.error('HTTP request error:', decodeUnicodeEscapes(data.statusText), data);
                }
            };
            window.chrome.webview.addEventListener('message', handler);
            const message = {
                type: 'httpRequest',
                requestId: requestId,
                url: url,
                method: options.method || 'GET',
                headers: options.headers || {},
                body: options.body,
                contentType: options.contentType || 'application/json'
            };
            window.chrome.webview.postMessage(message);
        });
    }
};