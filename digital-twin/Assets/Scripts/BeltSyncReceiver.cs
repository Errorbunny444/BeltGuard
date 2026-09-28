using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class BeltSyncReceiver : MonoBehaviour
{
    [Header("Network")]
    public int listenPort = 5066;

    [Header("References")]
    public ConveyorSpeedControl speedControl;

    [Header("Mapping")]
    [Tooltip("Maps real-world belt RPM to Unity ConvSpeed. 15 RPM -> 5.0 Unity speed.")]
    public float rpmToUnity = 0.33333334f;
    private UdpClient udp;
    private Thread thread;
    private volatile bool running;

    private readonly object messageLock = new object();
    private bool hasPendingSpeed;
    private float pendingConvSpeed;
    private float pendingSourceValue;
    private string pendingSourceLabel = "";

    private StartButton startButton;

    void Start()
    {
        if (speedControl == null)
        {
            speedControl = FindAnyObjectByType<ConveyorSpeedControl>();
        }

        if (speedControl != null)
        {
            startButton = speedControl.startButton;
        }

        if (startButton == null)
        {
            startButton = FindAnyObjectByType<StartButton>();
        }

        try
        {
            udp = new UdpClient(listenPort);
            running = true;

            thread = new Thread(ReceiveLoop)
            {
                IsBackground = true
            };
            thread.Start();

            Debug.Log("[BeltSync] Listening on port " + listenPort);
        }
        catch (SocketException ex)
        {
            Debug.LogError("[BeltSync] Could not start UDP listener: " + ex.Message);
        }
    }

    void Update()
    {
        bool apply;
        float convSpeed;
        float sourceValue;
        string sourceLabel;

        lock (messageLock)
        {
            apply = hasPendingSpeed;
            convSpeed = pendingConvSpeed;
            sourceValue = pendingSourceValue;
            sourceLabel = pendingSourceLabel;
            hasPendingSpeed = false;
        }

        if (!apply || speedControl == null || startButton == null)
        {
            return;
        }

        if (ProximitySyncReceiver.IsProximityDetected && !Mathf.Approximately(convSpeed, 0f))
        {
            return;
        }

        speedControl.ConvSpeed = convSpeed;
        startButton.SetRunning(!Mathf.Approximately(convSpeed, 0f));

        Debug.Log($"[BeltSync] {sourceLabel}:{sourceValue.ToString("0.###", CultureInfo.InvariantCulture)} -> ConvSpeed:{convSpeed.ToString("0.###", CultureInfo.InvariantCulture)}");
    }

    void OnDestroy()
    {
        running = false;

        try
        {
            udp?.Close();
        }
        catch
        {
        }

        if (thread != null && thread.IsAlive)
        {
            thread.Join(250);
        }
    }

    private void ReceiveLoop()
    {
        IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, listenPort);

        while (running)
        {
            try
            {
                byte[] data = udp.Receive(ref endPoint);
                string msg = Encoding.UTF8.GetString(data).Trim();

                if (TryParseConvSpeed(msg, out string sourceLabel, out float sourceValue, out float convSpeed))
                {
                    lock (messageLock)
                    {
                        pendingSourceLabel = sourceLabel;
                        pendingSourceValue = sourceValue;
                        pendingConvSpeed = convSpeed;
                        hasPendingSpeed = true;
                    }
                }
            }
            catch (SocketException)
            {
                if (!running)
                {
                    break;
                }
            }
            catch (System.Exception)
            {
            }
        }
    }

    private bool TryParseConvSpeed(string msg, out string sourceLabel, out float sourceValue, out float convSpeed)
    {
        sourceLabel = "";
        sourceValue = 0f;
        convSpeed = 0f;

        if (msg.StartsWith("BELT:"))
        {
            if (!float.TryParse(msg.Substring(5), NumberStyles.Float, CultureInfo.InvariantCulture, out sourceValue))
            {
                return false;
            }

            sourceLabel = "BELT";
            convSpeed = sourceValue * rpmToUnity;
            return true;
        }

        if (msg.StartsWith("SPEED:"))
        {
            if (!float.TryParse(msg.Substring(6), NumberStyles.Float, CultureInfo.InvariantCulture, out sourceValue))
            {
                return false;
            }

            sourceLabel = "SPEED";
            convSpeed = sourceValue / 15.0f;
            return true;
        }

        return false;
    }
}
