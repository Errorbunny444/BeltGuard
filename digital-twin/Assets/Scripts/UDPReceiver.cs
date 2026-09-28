using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class UDPReceiver : MonoBehaviour
{
    [Header("Network")]
    public int listenPort = 5065;

    [Header("Detection")]
    public float maxRevealDistance = 1.5f;

    private UdpClient udp;
    private Thread    thread;
    private volatile bool running;

    private readonly object messageLock = new object();
    private bool   hasPendingMessage;
    private string pendingMessage = "";

    void Start()
    {
        try
        {
            udp     = new UdpClient(listenPort);
            running = true;
            thread  = new Thread(ReceiveLoop) { IsBackground = true };
            thread.Start();
            Debug.Log("[UDPReceiver] Listening on port " + listenPort);
        }
        catch (SocketException ex)
        {
            Debug.LogError("[UDPReceiver] " + ex.Message);
        }
    }

    void Update()
    {
        string msg = null;
        lock (messageLock)
        {
            if (!hasPendingMessage) return;
            msg = pendingMessage;
            hasPendingMessage = false;
        }
        ProcessMessage(msg);
    }

    void OnDestroy()
    {
        running = false;
        try { udp?.Close(); } catch { }
        if (thread != null && thread.IsAlive) thread.Join(250);
    }

    private void ReceiveLoop()
    {
        IPEndPoint ep = new IPEndPoint(IPAddress.Any, listenPort);
        while (running)
        {
            try
            {
                byte[] data = udp.Receive(ref ep);
                string msg  = Encoding.UTF8.GetString(data).Trim();
                lock (messageLock) { pendingMessage = msg; hasPendingMessage = true; }
            }
            catch (SocketException) { if (!running) break; }
        }
    }

    private void ProcessMessage(string msg)
    {
        if (!ProximitySyncReceiver.IsProximityDetected)
        {
            Debug.Log("[UDPReceiver] Ignored — proximity not active");
            return;
        }

        // Parse chess piece colour
        if (!TryParseChessColour(msg, out Color pieceColor, out string label))
        {
            Debug.LogWarning("[UDPReceiver] Unknown message: " + msg);
            return;
        }

        // Find nearest uninspected bolt/piece
        BoltIdentity bolt = FindNearestPendingBolt();
        if (bolt == null)
        {
            Debug.LogWarning("[UDPReceiver] No pending piece found");
            return;
        }

        // Apply colour to the piece
        ApplyColour(bolt.gameObject, pieceColor);

        bolt.isInspected = true;
        bolt.hiddenType  = (label == "White")
                           ? BoltIdentity.BoltType.White
                           : BoltIdentity.BoltType.Black;
        bolt.RevealType();

        if (InspectionManager.Instance != null)
            InspectionManager.Instance.InspectBolt(bolt.gameObject);

        SimulationProximitySensor.MarkInspectionHandled();

        Debug.Log($"[UDPReceiver] Chess piece → {label}  colour applied ✅");
    }

    // Apply pure white or pure black to all renderers on the object
    private void ApplyColour(GameObject obj, Color color)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            // Create a new material instance to avoid shared material changes
            Material mat = new Material(r.material);
            mat.color = color;
            r.material = mat;
        }
    }

    private bool TryParseChessColour(string msg, out Color color, out string label)
    {
        color = Color.gray;
        label = "";

        string n = msg.Trim();
        if (n.StartsWith("BOLT:", true, CultureInfo.InvariantCulture))
            n = n.Substring(5);
        n = n.Trim().ToLowerInvariant();

        switch (n)
        {
            case "white":
            case "good":
            case "accept":
                color = Color.white; label = "White"; return true;

            case "black":
            case "bad":
            case "broken":
            case "reject":
                color = Color.black; label = "Black"; return true;

            default:
                return false;
        }
    }

    private BoltIdentity FindNearestPendingBolt()
    {
        BoltIdentity[] all     = FindObjectsByType<BoltIdentity>();
        BoltIdentity   nearest = null;
        float          minDist = float.MaxValue;

        foreach (BoltIdentity b in all)
        {
            if (b == null || b.isInspected || b.boltType != BoltIdentity.BoltType.Unknown)
                continue;
            float d = Vector3.Distance(b.transform.position, transform.position);
            if (d > maxRevealDistance || d >= minDist) continue;
            minDist = d; nearest = b;
        }
        return nearest;
    }
}
