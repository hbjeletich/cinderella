using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Server;
using System.Net;
using System.Net.Sockets; 
using System;
public class Server : MonoBehaviour
{
    public int port = 8080;
    private WebSocketServer wssv;
    public static Server Instance;
    private string localIP;

    public string LocalIP => localIP;

    public Action OnWSServerStarted;

    void Awake()
    {
        if(Instance == null)
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
        if (wssv != null)
        {
            wssv.Stop();
            wssv = null;
            Debug.Log("WebSocket server stopped.");

            OnWSServerStarted?.Invoke();
        }
    }

    public void StartServer()
    {
        if (wssv == null)
        {
            wssv = new WebSocketServer(port);
            // add behaviors here!
            wssv.AddWebSocketService<PlayerInput>("/");
            wssv.AddWebSocketService<PlayerInput>("/game");

            wssv.Start();
            Debug.Log($"WebSocket server started at ws://{localIP}:{port}/Server");
        }
    }



// found this method online to get local IP address
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

public class PlayerInput : WebSocketBehavior
{
    protected override void OnOpen()
    {
        Debug.Log($"[WS] Client connected: {ID}");
    }

    protected override void OnMessage(MessageEventArgs e)
    {
        Debug.Log($"Received message from client: {e.Data}");
    }
}
