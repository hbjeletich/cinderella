using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using UnityEngine;

namespace WebSocketServerDemo
{
    /// <summary>
    /// Simple HTTP server that serves the controller HTML file.
    /// Runs alongside the WebSocket server so browsers can access the controller page.
    /// </summary>
    public class HttpFileServer : MonoBehaviour
    {
        [Header("HTTP Server Settings")]
        [Tooltip("Port for the HTTP server (different from WebSocket port)")]
        public int httpPort = 8000;
        
        [Tooltip("The HTML file to serve (relative to StreamingAssets or absolute path)")]
        public string htmlFileName = "controller.html";
        
        [Header("Auto Start")]
        [Tooltip("Start the HTTP server automatically when the game runs")]
        public bool autoStart = true;
        
        [Header("Status")]
        [SerializeField] private bool isRunning = false;
        
        private HttpListener httpListener;
        private Thread listenerThread;
        private string htmlContent;
        private string localIP;
        
        private void Start()
        {
            localIP = GetLocalIPAddress();
            LoadHtmlFile();
            
            if (autoStart)
            {
                StartServer();
            }
        }
        
        private void OnDestroy()
        {
            StopServer();
        }
        
        private void OnApplicationQuit()
        {
            StopServer();
        }
        
        /// <summary>
        /// Start the HTTP server - can be called from a UI button
        /// </summary>
        public void StartServer()
        {
            if (isRunning)
            {
                Debug.Log("[HTTP] Server already running");
                return;
            }
            
            try
            {
                httpListener = new HttpListener();
                httpListener.Prefixes.Add($"http://*:{httpPort}/");
                httpListener.Start();
                
                listenerThread = new Thread(ListenForRequests);
                listenerThread.IsBackground = true;
                listenerThread.Start();
                
                isRunning = true;
                
                Debug.Log($"[HTTP] ✓ Server started!");
                Debug.Log($"[HTTP] ➜ Open on phone/browser: http://{localIP}:{httpPort}/");
                Debug.Log($"[HTTP] ➜ Or on this computer: http://localhost:{httpPort}/");
            }
            catch (Exception e)
            {
                Debug.LogError($"[HTTP] Failed to start server: {e.Message}");
                
                // Common issue: need admin rights on Windows for HttpListener
                if (e.Message.Contains("Access is denied"))
                {
                    Debug.LogError("[HTTP] Try running Unity as Administrator, or use a port above 1024");
                }
            }
        }
        
        /// <summary>
        /// Stop the HTTP server - can be called from a UI button
        /// </summary>
        public void StopServer()
        {
            if (!isRunning) return;
            
            try
            {
                isRunning = false;
                httpListener?.Stop();
                httpListener?.Close();
                listenerThread?.Abort();
                
                Debug.Log("[HTTP] Server stopped");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HTTP] Error stopping server: {e.Message}");
            }
        }
        
        /// <summary>
        /// Toggle server on/off - useful for a single button
        /// </summary>
        public void ToggleServer()
        {
            if (isRunning)
                StopServer();
            else
                StartServer();
        }
        
        /// <summary>
        /// Get the URL that phones should connect to
        /// </summary>
        public string GetConnectionURL()
        {
            return $"http://{localIP}:{httpPort}/";
        }
        
        private void ListenForRequests()
        {
            while (isRunning && httpListener != null && httpListener.IsListening)
            {
                try
                {
                    // Wait for a request
                    var context = httpListener.GetContext();
                    ProcessRequest(context);
                }
                catch (HttpListenerException)
                {
                    // Listener was stopped, exit gracefully
                    break;
                }
                catch (Exception e)
                {
                    if (isRunning)
                    {
                        Debug.LogWarning($"[HTTP] Request error: {e.Message}");
                    }
                }
            }
        }
        
        private void ProcessRequest(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;
            
            string requestPath = request.Url.AbsolutePath;
            Debug.Log($"[HTTP] Request: {request.HttpMethod} {requestPath} from {request.RemoteEndPoint}");
            
            byte[] buffer;
            
            // Serve the HTML file for root or controller.html requests
            if (requestPath == "/" || requestPath == "/controller.html" || requestPath == "/index.html")
            {
                response.ContentType = "text/html; charset=utf-8";
                
                // Inject the WebSocket server IP into the HTML
                string modifiedHtml = InjectServerIP(htmlContent);
                buffer = Encoding.UTF8.GetBytes(modifiedHtml);
            }
            // Simple favicon response to avoid 404s
            else if (requestPath == "/favicon.ico")
            {
                response.StatusCode = 204; // No content
                buffer = new byte[0];
            }
            else
            {
                // 404 for other requests
                response.StatusCode = 404;
                response.ContentType = "text/plain";
                buffer = Encoding.UTF8.GetBytes("Not Found");
            }
            
            response.ContentLength64 = buffer.Length;
            
            try
            {
                response.OutputStream.Write(buffer, 0, buffer.Length);
                response.OutputStream.Close();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HTTP] Response error: {e.Message}");
            }
        }
        
        /// <summary>
        /// Inject the server's IP address into the HTML so it auto-fills
        /// </summary>
        private string InjectServerIP(string html)
        {
            // Find the WebSocket server component to get its port
            int wsPort = 8080; // default
            var wsServer = GetComponent<TestServer>();
            if (wsServer != null)
            {
                // The port field is inherited from WebSocketServer base class
                wsPort = wsServer.port;
            }
            
            // Replace the empty value="" with our IP
            string modified = html.Replace(
                "id=\"serverIP\" placeholder=\"192.168.1.xxx\" value=\"\"",
                $"id=\"serverIP\" placeholder=\"192.168.1.xxx\" value=\"{localIP}\""
            );
            
            // Also set the correct WebSocket port
            modified = modified.Replace(
                "id=\"serverPort\" value=\"8080\"",
                $"id=\"serverPort\" value=\"{wsPort}\""
            );
            
            return modified;
        }
        
        private void LoadHtmlFile()
        {
            // Try multiple locations for the HTML file
            string[] possiblePaths = new string[]
            {
                // StreamingAssets (works in builds)
                Path.Combine(Application.streamingAssetsPath, htmlFileName),
                // Project root (editor only)
                Path.Combine(Application.dataPath, "..", "BrowserClient", htmlFileName),
                // Same folder as Assets
                Path.Combine(Application.dataPath, htmlFileName),
                // Resources approach would need different loading
            };
            
            foreach (string path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    htmlContent = File.ReadAllText(path);
                    Debug.Log($"[HTTP] Loaded HTML from: {path}");
                    return;
                }
            }
            
            // If no file found, use embedded HTML
            Debug.LogWarning($"[HTTP] HTML file not found, using embedded version");
            htmlContent = GetEmbeddedHtml();
        }
        
        private string GetLocalIPAddress()
        {
            try
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        if (ip.ToString().StartsWith("192.168."))
                        {
                            return ip.ToString();
                        }
                    }
                }
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[HTTP] Could not get local IP: {e.Message}");
            }
            return "localhost";
        }
        
        /// <summary>
        /// Embedded HTML in case the file isn't found - this is a minimal version
        /// </summary>
        private string GetEmbeddedHtml()
        {
            return @"<!DOCTYPE html>
<html lang=""en"">
<head>
    <meta charset=""UTF-8"">
    <meta name=""viewport"" content=""width=device-width, initial-scale=1.0, maximum-scale=1.0, user-scalable=no"">
    <title>Unity Controller</title>
    <style>
        * { box-sizing: border-box; }
        body {
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            background: linear-gradient(135deg, #1a1a2e 0%, #16213e 100%);
            color: #fff; margin: 0; padding: 20px; min-height: 100vh;
            display: flex; flex-direction: column; align-items: center;
        }
        .container { width: 100%; max-width: 500px; }
        h1 { text-align: center; margin-bottom: 10px; }
        .status {
            text-align: center; padding: 10px 20px; border-radius: 25px;
            margin-bottom: 20px; font-weight: 500;
        }
        .status.disconnected { background: rgba(255,82,82,0.2); border: 2px solid #ff5252; color: #ff5252; }
        .status.connecting { background: rgba(255,193,7,0.2); border: 2px solid #ffc107; color: #ffc107; }
        .status.connected { background: rgba(76,175,80,0.2); border: 2px solid #4caf50; color: #4caf50; }
        .panel { background: rgba(255,255,255,0.1); border-radius: 15px; padding: 20px; margin-bottom: 20px; }
        .input-group { display: flex; gap: 10px; margin-bottom: 15px; }
        .input-group .field { flex: 1; }
        .input-group .field.port { flex: 0 0 80px; }
        .input-group label { display: block; margin-bottom: 5px; font-size: 0.9rem; opacity: 0.8; }
        input {
            width: 100%; padding: 12px 15px; border: 2px solid rgba(255,255,255,0.2);
            border-radius: 10px; background: rgba(255,255,255,0.1); color: #fff; font-size: 1rem;
        }
        input:focus { border-color: #64b5f6; outline: none; }
        button {
            width: 100%; padding: 15px; border: none; border-radius: 10px;
            font-size: 1rem; font-weight: 600; cursor: pointer;
        }
        .btn-connect { background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; }
        .btn-disconnect { background: linear-gradient(135deg, #ff5252 0%, #f44336 100%); color: white; }
        .btn-send { background: linear-gradient(135deg, #4caf50 0%, #45a049 100%); color: white; margin-top: 10px; }
        button:disabled { opacity: 0.5; cursor: not-allowed; }
        .quick-buttons { display: grid; grid-template-columns: repeat(2, 1fr); gap: 10px; margin-top: 15px; }
        .quick-btn {
            padding: 20px; background: rgba(255,255,255,0.15); border: 2px solid rgba(255,255,255,0.2);
            color: white; font-size: 1.5rem;
        }
        .quick-btn:active { background: rgba(255,255,255,0.3); transform: scale(0.95); }
    </style>
</head>
<body>
    <div class=""container"">
        <h1>🎮 Unity Controller</h1>
        <div id=""status"" class=""status disconnected"">● Disconnected</div>
        
        <div class=""panel"">
            <div class=""input-group"">
                <div class=""field"">
                    <label>Server IP</label>
                    <input type=""text"" id=""serverIP"" placeholder=""192.168.1.xxx"" value="""">
                </div>
                <div class=""field port"">
                    <label>Port</label>
                    <input type=""number"" id=""serverPort"" value=""8080"">
                </div>
            </div>
            <button id=""connectBtn"" class=""btn-connect"" onclick=""toggleConnection()"">Connect</button>
        </div>
        
        <div class=""panel"">
            <input type=""text"" id=""msgInput"" placeholder=""Type a message..."" disabled>
            <button class=""btn-send"" onclick=""sendMsg()"" disabled id=""sendBtn"">Send</button>
            
            <div class=""quick-buttons"">
                <button class=""quick-btn"" onclick=""send('⬆️')"" disabled>⬆️</button>
                <button class=""quick-btn"" onclick=""send('⬇️')"" disabled>⬇️</button>
                <button class=""quick-btn"" onclick=""send('⬅️')"" disabled>⬅️</button>
                <button class=""quick-btn"" onclick=""send('➡️')"" disabled>➡️</button>
                <button class=""quick-btn"" onclick=""send('🅰️')"" disabled>🅰️</button>
                <button class=""quick-btn"" onclick=""send('🅱️')"" disabled>🅱️</button>
            </div>
        </div>
    </div>

    <script>
        let ws = null;
        const setEnabled = (on) => {
            document.getElementById('msgInput').disabled = !on;
            document.getElementById('sendBtn').disabled = !on;
            document.querySelectorAll('.quick-btn').forEach(b => b.disabled = !on);
        };
        const setStatus = (cls, txt) => {
            const s = document.getElementById('status');
            s.className = 'status ' + cls;
            s.textContent = txt;
        };
        function toggleConnection() {
            if (ws && ws.readyState === WebSocket.OPEN) {
                ws.close();
            } else {
                const ip = document.getElementById('serverIP').value;
                const port = document.getElementById('serverPort').value;
                if (!ip) { alert('Enter server IP'); return; }
                setStatus('connecting', '● Connecting...');
                ws = new WebSocket('ws://' + ip + ':' + port);
                ws.onopen = () => {
                    setStatus('connected', '● Connected');
                    document.getElementById('connectBtn').textContent = 'Disconnect';
                    document.getElementById('connectBtn').className = 'btn-disconnect';
                    setEnabled(true);
                };
                ws.onclose = () => {
                    setStatus('disconnected', '● Disconnected');
                    document.getElementById('connectBtn').textContent = 'Connect';
                    document.getElementById('connectBtn').className = 'btn-connect';
                    setEnabled(false);
                };
                ws.onerror = () => setStatus('disconnected', '● Error');
            }
        }
        function send(msg) {
            if (ws && ws.readyState === WebSocket.OPEN) {
                ws.send(JSON.stringify({type:'input',input:msg}));
            }
        }
        function sendMsg() {
            const input = document.getElementById('msgInput');
            if (input.value.trim()) {
                send(input.value.trim());
                input.value = '';
            }
        }
        document.getElementById('msgInput').addEventListener('keypress', e => { if(e.key==='Enter') sendMsg(); });
    </script>
</body>
</html>";
        }
    }
}