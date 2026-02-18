const webWrap = new WebWrapClient();
const $ = id => document.getElementById(id);

async function send() {
  const url = $('url').value;
  const method = $('method').value;
  let headers = {};
  try { headers = $('headers').value.trim() ? JSON.parse($('headers').value) : {}; } catch(e){ alert('Invalid headers JSON'); return; }
  const bodyText = $('body').value;

  const options = { method, headers };
  if (bodyText) {
    // try parse JSON
    try { options.body = JSON.stringify(JSON.parse(bodyText)); } catch { options.body = bodyText; }
  }
  
  $('status').textContent = 'Sending...';
  $('response').textContent = '';

  try {
    // use the web wrapper proxy to make the HTTP request via the .NET side, 
    // which allows us to bypass CORS and other browser limitations
    //url = the URL to request, options = { method, headers, body, contentType }
     const res = await webWrap.ProxyFetch(url, options);

    const statusText = `Status: ${res.status} ${res.statusText || ''}`;
    $('status').textContent = statusText;

    // try to show JSON prettily if possible
    let text = await res.text();
    try {
      const js = JSON.parse(text);
      text = JSON.stringify(js, null, 2);
    } catch(_) {}

    $('response').textContent = text || '(empty body)';
  } catch (err) {
    $('status').textContent = 'Error';
    $('response').textContent = String(err);
  }
}

$('send').addEventListener('click', send);

$('exampleGet').addEventListener('click', () => {
  $('url').value = 'https://jsonplaceholder.typicode.com/posts/1';
  $('method').value = 'GET';
  $('headers').value = '';
  $('body').value = '';
});

$('examplePost').addEventListener('click', () => {
  $('url').value = 'https://jsonplaceholder.typicode.com/posts';
  $('method').value = 'POST';
  $('headers').value = JSON.stringify({ 'Content-Type': 'application/json' }, null, 2);
  $('body').value = JSON.stringify({ title: 'foo', body: 'bar', userId: 1 }, null, 2);
});

$('copy').addEventListener('click', async () => {
  try { await navigator.clipboard.writeText($('response').textContent); } catch { /* ignore */ }
});
