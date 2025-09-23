using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Concurrent;
using TMPro;

public class LANNetworkManager : MonoBehaviour
{
    [Header("Ports")]
    public int broadcastPort = 47777;   // discovery
    public ushort gamePort = 7777;      // actual gameplay

    [Header("UI Debugging")]
    public TextMeshProUGUI debugText;   // assign in inspector
    public int maxLines = 50;

    private Thread broadcastThread;
    private Thread receiveThread;
    private CancellationTokenSource cts;

    private ConcurrentQueue<string> logQueue = new ConcurrentQueue<string>();
    private ConcurrentQueue<System.Action> mainThreadActions = new ConcurrentQueue<System.Action>();

    // ---------------- API ----------------
    public void StartHost()
    {
        Log("[LAN] Starting Host...");

        var transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
        transport.ConnectionData.Address = "0.0.0.0";
        transport.ConnectionData.Port = gamePort;

        if (NetworkManager.Singleton.StartHost())
        {
            Log("[LAN] Host started");
            StartBroadcast();
        }
        else
        {
            Log("[LAN][ERROR] Failed to start host");
        }
    }

    public void StartClient()
    {
        Log("[LAN] Starting Client...");
        StartListening();
    }

    // ---------------- Threads ----------------
    private void StartBroadcast()
    {
        StopThreads();
        cts = new CancellationTokenSource();
        broadcastThread = new Thread(() => BroadcastLoop(cts.Token)) { IsBackground = true };
        broadcastThread.Start();
    }

    private void StartListening()
    {
        StopThreads();
        cts = new CancellationTokenSource();
        receiveThread = new Thread(() => ReceiveLoop(cts.Token)) { IsBackground = true };
        receiveThread.Start();
    }

    private void BroadcastLoop(CancellationToken token)
    {
        using (var udp = new UdpClient())
        {
            udp.EnableBroadcast = true;
            IPEndPoint ep = new IPEndPoint(IPAddress.Broadcast, broadcastPort);
            string msg = $"HOST|{GetLocalIP()}|{gamePort}";
            byte[] data = Encoding.UTF8.GetBytes(msg);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    udp.Send(data, data.Length, ep);
                    Log($"[LAN] Broadcasting host on {ep} : {msg}");
                }
                catch (System.Exception ex)
                {
                    Log("[LAN][ERROR] Broadcast failed: " + ex.Message);
                }

                Thread.Sleep(1000);
            }
        }
    }

    private void ReceiveLoop(CancellationToken token)
    {
        using (var udp = new UdpClient(broadcastPort))
        {
            IPEndPoint remote = new IPEndPoint(IPAddress.Any, 0);

            while (!token.IsCancellationRequested)
            {
                try
                {
                    byte[] data = udp.Receive(ref remote);
                    string msg = Encoding.UTF8.GetString(data);
                    Log($"[LAN] Received: {msg} from {remote}");

                    if (msg.StartsWith("HOST|"))
                    {
                        string[] parts = msg.Split('|');
                        if (parts.Length == 3)
                        {
                            string hostIP = parts[1];
                            ushort port = ushort.Parse(parts[2]);

                            mainThreadActions.Enqueue(() =>
                            {
                                var transport = (UnityTransport)NetworkManager.Singleton.NetworkConfig.NetworkTransport;
                                transport.ConnectionData.Address = hostIP;
                                transport.ConnectionData.Port = port;

                                NetworkManager.Singleton.StartClient();
                                Log($"[LAN] Connecting to host {hostIP}:{port}");
                            });

                            break;
                        }
                    }
                }
                catch (SocketException) { }
                catch (System.Exception ex)
                {
                    Log("[LAN][ERROR] Receive failed: " + ex.Message);
                }
            }
        }
    }

    // ---------------- Utilities ----------------
    void Update()
    {
        while (mainThreadActions.TryDequeue(out var action))
            action.Invoke();

        while (logQueue.TryDequeue(out var line))
            AppendLog(line);
    }

    private void Log(string msg)
    {
        logQueue.Enqueue($"[{System.DateTime.Now:HH:mm:ss}] {msg}");
        Debug.Log(msg);
    }

    private void AppendLog(string line)
    {
        if (!debugText) return;

        debugText.text += line + "\n";

        // trim lines
        var lines = debugText.text.Split('\n');
        if (lines.Length > maxLines)
        {
            debugText.text = string.Join("\n", lines, lines.Length - maxLines, maxLines);
        }
    }

    private void StopThreads()
    {
        if (cts != null)
        {
            cts.Cancel();
            cts.Dispose();
            cts = null;
        }
    }

    private string GetLocalIP()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                return ip.ToString();
        return "127.0.0.1";
    }

    private void OnDestroy() => StopThreads();
    private void OnApplicationQuit() => StopThreads();
}
