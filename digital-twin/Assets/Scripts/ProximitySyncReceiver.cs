using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class ProximitySyncReceiver : MonoBehaviour
{
    public static bool IsProximityDetected { get; private set; }

    [Header("Network")]
    public int listenPort = 5069;

    [Header("References")]
    public ConveyorSpeedControl speedControl;
    public Renderer sensorRenderer;

    private UdpClient udp;
    private Thread thread;
    private volatile bool running;

    private readonly object messageLock = new object();
    private bool hasPendingMessage;
    private string pendingMessage = "";

    private float savedSpeedBeforeStop = 0f;

    void Start()
    {
        if (speedControl == null)
        {
            speedControl = FindAnyObjectByType<ConveyorSpeedControl>();
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

            SetSensorColor(Color.cyan);
            Debug.Log("[Prox] Listening on port " + listenPort);
        }
        catch (SocketException ex)
        {
            Debug.LogError("[Prox] Could not start UDP listener: " + ex.Message);
        }
    }

    void Update()
    {
        string msg = null;

        lock (messageLock)
        {
            if (!hasPendingMessage)
            {
                return;
            }

            msg = pendingMessage;
            hasPendingMessage = false;
        }

        if (msg == "PROX:DETECTED")
        {
            HandleDetected();
        }
        else if (msg == "PROX:CLEAR")
        {
            HandleClear();
        }
    }

    void OnDestroy()
    {
        running = false;
        IsProximityDetected = false;

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

                if (msg != "PROX:DETECTED" && msg != "PROX:CLEAR")
                {
                    continue;
                }

                lock (messageLock)
                {
                    pendingMessage = msg;
                    hasPendingMessage = true;
                }
            }
            catch (SocketException)
            {
                if (!running)
                {
                    break;
                }
            }
        }
    }

    private void HandleDetected()
    {
        if (IsProximityDetected || speedControl == null || speedControl.startButton == null)
        {
            return;
        }

        savedSpeedBeforeStop = speedControl.ConvSpeed;
        IsProximityDetected = true;

        speedControl.ConvSpeed = 0f;
        speedControl.startButton.StopBelt();
        SetSensorColor(Color.red);

        Debug.Log("[Prox] Object detected -> Unity belt stopped");
    }

    private void HandleClear()
    {
        if (!IsProximityDetected || speedControl == null || speedControl.startButton == null)
        {
            return;
        }

        IsProximityDetected = false;

        speedControl.ConvSpeed = savedSpeedBeforeStop;
        speedControl.startButton.SetRunning(!Mathf.Approximately(savedSpeedBeforeStop, 0f));
        SetSensorColor(Color.cyan);

        Debug.Log("[Prox] Belt resume acknowledged");
    }

    private void SetSensorColor(Color color)
    {
        if (sensorRenderer != null)
        {
            sensorRenderer.material.color = color;
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(0.5f, 0.3f, 2f));
        Gizmos.color = new Color(0f, 1f, 1f, 0.2f);
        Gizmos.DrawCube(transform.position, new Vector3(0.5f, 0.3f, 2f));
    }
}
