using System.Collections;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

// ============================================
// SimulationProximitySensor.cs
//
// IMPORTANT:
// Real World Proximity = MASTER
// This simulation sensor is DISABLED by default.
// Set simulationActive = true ONLY for testing
// without real hardware connected.
//
// In production: real sensor fires → Arduino →
// belt_sync.py → PROX:DETECTED → Unity stops.
// ============================================

public class SimulationProximitySensor : MonoBehaviour
{
    public static bool IsSimulationModeActive { get; private set; }
    public static bool WaitingForInspection   { get; private set; }

    [Header("Mode")]
    [Tooltip("FALSE = Real world prox is master (production). TRUE = Unity simulates prox (testing only).")]
    public bool simulationActive = false;   // ← DEFAULT OFF — real world prox is master

    [Header("Network")]
    public string targetIp      = "127.0.0.1";
    public int    proximityPort = 5069;
    public int    triggerPort   = 5070;

    [Header("Detection")]
    public float fallbackResumeDelay = 5f;
    public float detectionDistance   = 0.6f;

    private bool      waitingForExit = false;
    private Collider  sensorCollider;

    void Start()
    {
        IsSimulationModeActive = simulationActive;
        sensorCollider         = GetComponent<Collider>();

        if (!simulationActive)
            Debug.Log("[SimSensor] DISABLED — Real world proximity sensor is master");
        else
            Debug.Log("[SimSensor] ACTIVE — Simulation mode (testing only)");
    }

    void Update()
    {
        // Do nothing if simulation is off — real world prox handles everything
        if (!simulationActive || waitingForExit || WaitingForInspection)
            return;

        BoltIdentity[] bolts = FindObjectsByType<BoltIdentity>(FindObjectsSortMode.None);
        foreach (BoltIdentity bolt in bolts)
        {
            if (bolt == null || bolt.boltType != BoltIdentity.BoltType.Unknown)
                continue;

            if (!IsBoltInsideSensor(bolt))
                continue;

            TriggerInspection();
            break;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (!simulationActive || waitingForExit || WaitingForInspection)
            return;

        BoltIdentity bolt = other.GetComponent<BoltIdentity>();
        if (bolt == null) return;

        TriggerInspection();
    }

    void OnTriggerExit(Collider other)
    {
        BoltIdentity bolt = other.GetComponent<BoltIdentity>();
        if (bolt == null) return;
        waitingForExit = false;
    }

    void OnDestroy()
    {
        IsSimulationModeActive = false;
        WaitingForInspection   = false;
    }

    public static void MarkInspectionHandled()
    {
        WaitingForInspection = false;
    }

    bool IsBoltInsideSensor(BoltIdentity bolt)
    {
        Collider boltCollider = bolt.GetComponent<Collider>();
        if (sensorCollider != null && boltCollider != null)
        {
            if (sensorCollider.bounds.Intersects(boltCollider.bounds))
                return true;
        }
        return Vector3.Distance(transform.position, bolt.transform.position) <= detectionDistance;
    }

    void TriggerInspection()
    {
        waitingForExit   = true;
        WaitingForInspection = true;
        SendUdp("PROX:DETECTED", proximityPort);
        SendUdp("TRIGGER",       triggerPort);
        StartCoroutine(FallbackResumeRoutine());
        Debug.Log("[SimSensor] Simulation bolt detected → PROX:DETECTED + TRIGGER");
    }

    IEnumerator FallbackResumeRoutine()
    {
        yield return new WaitForSeconds(fallbackResumeDelay);

        if (!simulationActive || !WaitingForInspection)
            yield break;

        SendUdp("PROX:CLEAR", proximityPort);
        MarkInspectionHandled();
        Debug.Log("[SimSensor] Fallback PROX:CLEAR sent");
    }

    void SendUdp(string message, int port)
    {
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(message);
            using (UdpClient client = new UdpClient())
                client.Send(data, data.Length, targetIp, port);
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[SimSensor] UDP send failed: " + ex.Message);
        }
    }
}