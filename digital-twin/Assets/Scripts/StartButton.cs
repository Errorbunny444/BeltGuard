using UnityEngine;
using UnityEngine.InputSystem;
using System.Net.Sockets;
using System.Text;

// Shared simulation run/stop state for the existing belt and pulley scripts.
// Hardware transmission is opt-in and disabled by the health scene setup.
// The local rupture latch blocks all public start methods.

public class StartButton : MonoBehaviour
{
    [Header("Belt State")]
    public bool start = false;
    public bool emergencyStop = false;
    public bool sendHardwareCommands = false;
    public bool allowKeyboardToggle = true;

    [Header("Python Connection")]
    public string pythonIP   = "127.0.0.1";
    public int    pythonPort = 5067;          // Unity → belt_sync.py

    private bool lastStart = false;

    void Start()
    {
        start     = false;
        lastStart = false;
        SendToPython("STOP");
        Debug.Log("[StartButton] Initialized → Belt OFF");
    }

    void Update()
    {
        // SPACE key for manual testing
        if (emergencyStop) start = false;
        if (allowKeyboardToggle && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Toggle();
        }

        // ✅ Detect ANY change to start flag from ANY source
        // Covers: Toggle(), StartBelt(), StopBelt(), SetRunning()
        // Ensures Python always gets notified
        if (start != lastStart)
        {
            lastStart = start;

            if (start)
            {
                SendToPython("START");
                Debug.Log("🟢 [StartButton] Simulation RUNNING");
            }
            else
            {
                SendToPython("STOP");
                Debug.Log("🔴 [StartButton] Simulation STOPPED");
            }
        }
    }

    // ── Send UDP to belt_sync.py ─────────────────────────
    void SendToPython(string message)
    {
        if (!sendHardwareCommands) return;
        try
        {
            UdpClient udp  = new UdpClient();
            byte[]    data = Encoding.UTF8.GetBytes(message);
            udp.Send(data, data.Length, pythonIP, pythonPort);
            udp.Close();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[StartButton] UDP send failed: {e.Message}");
        }
    }

    // ── Called by UI button or SPACE key ─────────────────
    public void Toggle()
    {
        start = !emergencyStop && !start;
        // Update() detects change and sends UDP automatically
    }

    // ── Called by ProximitySyncReceiver (real world prox) ─
    // When real sensor fires → belt_sync.py → Unity stops belt
    public void SetRunning(bool isRunning)
    {
        start = isRunning && !emergencyStop;
        // Update() detects change and sends UDP automatically
    }

    // ── Convenience wrappers ──────────────────────────────
    public void StartBelt()  { SetRunning(true); }
    public void StopBelt()   { start = false; }
}
