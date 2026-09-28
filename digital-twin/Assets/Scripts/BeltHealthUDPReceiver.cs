using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class BeltHealthUDPReceiver : MonoBehaviour
{
    [Header("Local Python bridge")]
    [Tooltip("Localhost UDP port. Must match the Python sender; default 5055.")]
    public int port = 5055;
    public JointHealthManager manager;
    public bool acceptPackets = true;
    [Header("Runtime diagnostics")]
    public string connectionStatus = "Stopped";
    public int rejectedPackets;
    private UdpClient socket;
    private Thread worker;
    private volatile bool running;
    private readonly ConcurrentQueue<string> inbox = new ConcurrentQueue<string>();
    private string receiveError;

    void OnEnable()
    {
        try
        {
            socket = new UdpClient(new IPEndPoint(IPAddress.Loopback, port));
            socket.Client.ReceiveTimeout = 500;
            running = true;
            worker = new Thread(Receive) { IsBackground = true, Name = "Belt health UDP" };
            worker.Start();
            connectionStatus = $"Listening on localhost:{port}";
        }
        catch (Exception e)
        {
            // Release a socket acquired before a later initialization step failed.
            running = false;
            socket?.Close(); socket = null;
            connectionStatus = e.Message;
            Debug.LogError("[BeltHealth] " + e.Message);
        }
    }

    // No Unity API, JSON parsing, transforms or UI on this thread.
    void Receive()
    {
        var endpoint = new IPEndPoint(IPAddress.Any, 0);
        while (running)
        {
            try
            {
                byte[] bytes = socket.Receive(ref endpoint);
                if (bytes.Length <= 8192 && inbox.Count < 256)
                    inbox.Enqueue(Encoding.UTF8.GetString(bytes));
                else Interlocked.Exchange(ref receiveError, "Oversized packet or receive queue overflow");
            }
            catch (SocketException e)
            {
                if (running && e.SocketErrorCode != SocketError.TimedOut)
                    Interlocked.Exchange(ref receiveError, e.Message);
            }
            catch (ObjectDisposedException) { break; }
        }
    }

    public void ClearPending() { while (inbox.TryDequeue(out _)) { } }

    void Update()
    {
        var error = Interlocked.Exchange(ref receiveError, null);
        if (error != null) { connectionStatus = error; Debug.LogWarning("[BeltHealth] " + error); }
        for (int i = 0; i < 32 && inbox.TryDequeue(out string json); i++)
        {
            if (!acceptPackets) continue;
            try
            {
                // FromJsonOverwrite preserves -1 for omitted health_score, including IL2CPP.
                var data = new BeltHealthData {
                    belt_speed = float.NaN, motor_current = float.NaN, vibration = float.NaN,
                    temperature = float.NaN, encoder_position = float.NaN,
                    damage_severity = float.NaN, confidence = float.NaN, belt_position = float.NaN
                };
                JsonUtility.FromJsonOverwrite(json, data);
                if (!manager || !manager.Apply(data)) { rejectedPackets++; continue; }
                connectionStatus = "Receiving valid JSON";
            }
            catch (Exception) { rejectedPackets++; }
        }
    }

    void OnDisable()
    {
        running = false;
        socket?.Close();
        worker?.Join(1000);
        socket = null;
        ClearPending();
        connectionStatus = "Stopped";
    }
}
