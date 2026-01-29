using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace WebSocketServerDemo
{
    public class TestServer : WebSocketServer.WebSocketServer
    {
        [Header("UI Settings")]
        public TMP_Text displayText;
        public int maxMessages = 10;
        
        [Header("Server Settings")]
        // inherited from base class
        private Queue<string> messageHistory = new Queue<string>();
        
        // track connected clients
        private Dictionary<string, string> connectedClients = new Dictionary<string, string>();
        
        private void Start()
        {
            // display connection info
            string localIP = GetLocalIPAddress();
            string connectionInfo = $"Server running!\nConnect browsers to:\nws://{localIP}:{port}";
            
            Debug.Log(connectionInfo);
            AddMessage($"[SERVER] Started on {localIP}:{port}");
            AddMessage("[SERVER] Waiting for browser connections...");
        }
        
        public override void OnOpen(WebSocketServer.WebSocketConnection connection)
        {
            string clientId = connection.id;
            connectedClients[clientId] = clientId;
            
            Debug.Log($"[SERVER] Client connected: {clientId}");
            AddMessage($"[CONNECTED] Client {clientId.Substring(0, 8)}...");
            
            // Send welcome message to the client
            connection.Send($"{{\"type\":\"welcome\",\"message\":\"Connected to Unity server!\",\"clientId\":\"{clientId}\"}}");
        }
        
        public override void OnMessage(WebSocketServer.WebSocketMessage message)
        {
            string clientId = message.connection.id;
            string data = message.data;
            
            Debug.Log($"[SERVER] Message from {clientId}: {data}");
            
            // Try to parse as JSON, otherwise treat as plain text
            string displayMessage = ParseMessage(clientId, data);
            AddMessage(displayMessage);
            
            // Echo back confirmation to the sender
            message.connection.Send($"{{\"type\":\"received\",\"echo\":\"{data}\"}}");
            
            // Optionally broadcast to all clients
            BroadcastMessage($"{{\"type\":\"broadcast\",\"from\":\"{clientId}\",\"message\":\"{data}\"}}");
        }
        
        public override void OnClose(WebSocketServer.WebSocketConnection connection)
        {
            string clientId = connection.id;
            connectedClients.Remove(clientId);
            
            Debug.Log($"[SERVER] Client disconnected: {clientId}");
            AddMessage($"[DISCONNECTED] Client {clientId.Substring(0, 8)}...");
        }

        private string ParseMessage(string clientId, string data)
        {
            // Simple JSON parsing - you might want to use JsonUtility for complex messages
            if (data.StartsWith("{") && data.Contains("\"input\""))
            {
                // Try to extract "input" field from JSON
                try
                {
                    var wrapper = JsonUtility.FromJson<InputMessage>(data);
                    if (!string.IsNullOrEmpty(wrapper.input))
                    {
                        return $"[{clientId.Substring(0, 6)}] {wrapper.input}";
                    }
                }
                catch { }
            }
            
            // Return as plain text
            return $"[{clientId.Substring(0, 6)}] {data}";
        }
        
        private void AddMessage(string message)
        {
            messageHistory.Enqueue(message);
            
            // Keep only the last N messages
            while (messageHistory.Count > maxMessages)
            {
                messageHistory.Dequeue();
            }
            
            UpdateDisplay();
        }
        
        private void UpdateDisplay()
        {
            string displayContent = string.Join("\n", messageHistory.ToArray());
            
            if (displayText != null)
            {
                displayText.text = displayContent;
            }
        }
        
        public void BroadcastMessage(string message)
        {
            // Note: You'll need to implement this based on the WebSocketServer API
            // The base class should have a way to iterate connections
            Debug.Log($"[SERVER] Broadcasting: {message}");
        }
        
        private string GetLocalIPAddress()
        {
            try
            {
                var host = System.Net.Dns.GetHostEntry(System.Net.Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        // Prefer 192.168.x.x addresses for local network
                        if (ip.ToString().StartsWith("192.168."))
                        {
                            return ip.ToString();
                        }
                    }
                }
                // Fallback to first IPv4
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Could not get local IP: {e.Message}");
            }
            return "localhost";
        }
        
        [System.Serializable]
        private class InputMessage
        {
            public string input;
            public string type;
        }
    }
}