using UnityEngine;
using System;
using System.Net;
using System.Net.Sockets; 
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class HTTPServer : MonoBehaviour
{
    public int port = 8000;
    public string htmlName = "index.html";

    private HttpListener httpListener;
    private string localIP;
    private string htmlContent;

    // listener thread to handle requests
    private Thread listenerThread;

    // WS values
    private string wsAddress;
    private int wsPort = 8080;

    public Action OnHTTPServerStarted;

    void Start()
    {
        localIP = GetLocalIPAddress();
        GetWSValues();

        StartServer();

        LoadHTMLFiles();
    }

    private void GetWSValues()
    {
        // store these values early
        wsAddress = localIP;
        wsPort = 8080;

        Server serverInstance = Server.Instance;
        if (serverInstance != null)
        {
            wsAddress = serverInstance.LocalIP;
            wsPort = serverInstance.port;
        }

        Debug.Log($"WebSocket Address: {wsAddress}, Port: {wsPort}");
    }

    void StartServer()
    {
        if (httpListener == null)
        {
            httpListener = new HttpListener();
            httpListener.Prefixes.Add($"http://*:{port}/");
            httpListener.Start();
            Debug.Log($"HTTP server started at http://{localIP}:{port}/");

            listenerThread = new Thread(ListenForRequests);
            listenerThread.IsBackground = true;
            listenerThread.Start();

            OnHTTPServerStarted?.Invoke();
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

    private void StopServer()
    {
        if (httpListener != null)
        {
            httpListener.Stop();
            httpListener = null;

            // i found this online i hope it works i am out of my depth here
            if (listenerThread != null && listenerThread.IsAlive)
            {
                listenerThread.Join(1000);
            }
        }
    }

    private void ListenForRequests()
    {
        while (httpListener != null && httpListener.IsListening)
        {
            try
            {
                var context = httpListener.GetContext();
                ProcessRequest(context);
            }
            catch (Exception ex)
            {
                break;
            }
        }
    }

    private void ProcessRequest(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;

        // tell browser HTML
        response.ContentType = "text/html";

        // convert string to bytes
        byte[] buffer = Encoding.UTF8.GetBytes(htmlContent);

        // tell browser how many bytes to expect and send
        response.ContentLength64 = buffer.Length;
        response.OutputStream.Write(buffer, 0, buffer.Length);
        response.OutputStream.Close();
    }

    private void LoadHTMLFiles()
    {
        string path = Path.Combine(Application.streamingAssetsPath, htmlName);
        htmlContent = File.ReadAllText(path);
    }

    public void SwapHTML(string newHTML)
    {
        htmlName = newHTML;
        LoadHTMLFiles();
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
