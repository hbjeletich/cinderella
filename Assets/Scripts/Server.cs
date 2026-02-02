using UnityEngine;
using Fleck;
using System.Collections.Generic;
using System;
using System.Net;
using System.Net.Sockets;

public class Server : MonoBehaviour
{
    public int port = 8080;
    private WebSocketServer server;
    public static Server Instance;
    private string localIP;

    // store connections for ConnectionManager
    public static Dictionary<string, IWebSocketConnection> Connections = new Dictionary<string, IWebSocketConnection>();

    public string LocalIP => localIP;
    public Action OnWSServerStarted;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            localIP = GetLocalIPAddress();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        StartServer();
    }

    private void OnDestroy()
    {
        StopServer();
    }

    private void OnApplicationQuit()
    {
        StopServer();
    }

    public void StopServer()
    {
        if (server != null)
        {
            server.Dispose();
            server = null;
            Connections.Clear();
            Debug.Log("Server: WebSocket server stopped.");
        }
    }

    public void StartServer()
    {
        if (server == null)
        {
            server = new WebSocketServer($"ws://0.0.0.0:{port}");

            server.Start(socket =>
            {
                string id = socket.ConnectionInfo.Id.ToString();

                // note to self:
                // these events are called on a different thread, so be careful with Unity API calls
                // use MainThreadDispatcher if needing to interact with Unity objects
                socket.OnOpen = () =>
                {
                    Debug.Log($"Server: Client connected: {id}");
                    Connections[id] = socket;

                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        // create a new Player for this connection
                        PlayerManager.Instance.CreatePlayer(id);
                    });
                };

                socket.OnMessage = message =>
                {
                    Debug.Log($"Server: Received message from client: {message}");
                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {  
                        // handle the message on the main thread
                        ConnectionManager.Instance.HandleWebSocketMessage(message, id);
                    });
                };

                socket.OnClose = () =>
                {
                    Debug.Log($"Server: Client disconnected: {id}");
                    Connections.Remove(id);

                    MainThreadDispatcher.Instance.Enqueue(() =>
                    {
                        // remove player
                        PlayerManager.Instance.RemovePlayer(id);
                    });
                };

                socket.OnError = error =>
                {
                    Debug.LogError($"Server: Error: {error}");
                };
            });

            Debug.Log($"Server: WebSocket server started at ws://{localIP}:{port}");
            OnWSServerStarted?.Invoke();
        }
    }

    public static string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        return null;
    }
}