using System;
using UnityEngine;

public class JointHealthManager : MonoBehaviour
{
    [Header("Prototype normal / severe calibration (replace with measured baselines)")]
    public Vector2 vibrationLimits = new Vector2(1, 10); // mm/s RMS
    public Vector2 temperatureLimits = new Vector2(30, 80); // degrees C
    public Vector2 currentLimits = new Vector2(1, 5); // A
    public BeltRuptureStateManager rupture;
    public DamageVisualizer visualizer;
    public bool demoMode = true;
    public float staleAfterSeconds = 5;
    public BeltHealthData[] joints = new BeltHealthData[5];
    public BeltHealthData latest;
    public string dataStatus = "Awaiting telemetry";
    public string inputSource = "None";
    public string activeJointId = "J-03";
    // UI and inspection camera share one rule; a latched failure remains in view.
    public string DisplayJointId => rupture && rupture.ruptureEvent && IsJointId(rupture.ruptureJointId)
        ? rupture.ruptureJointId : activeJointId;
    public void SelectJoint(string id) { if (IsJointId(id)) activeJointId = id; }
    public JointHealthStore store;
    public bool HasFreshTelemetry => lastPacketTime >= 0 && Time.unscaledTime - lastPacketTime <= staleAfterSeconds;
    private float lastPacketTime = -1;

    public static bool IsJointId(string id)
        => id == "J-01" || id == "J-02" || id == "J-03" || id == "J-04" || id == "J-05";

    void Awake() { Initialize(); }

    public void Initialize()
    {
        joints = new BeltHealthData[5];
        for (int i = 0; i < 5; i++)
            joints[i] = new BeltHealthData { joint_id = $"J-{i + 1:00}", health_score = 100,
                risk_level = "healthy", damage_type = "none", maintenance_action = "Awaiting inspection",
                current_condition = "Healthy", maintenance_status = "Awaiting inspection",
                inspection_status = "Not inspected", last_updated = "Never (initial value)" };
    }

    public bool Apply(BeltHealthData packet, string source = "UDP", bool record = true)
    {
        if (packet == null || !packet.IsValid() || (!demoMode && packet.data_source == "simulator")) return false;
        var data = packet.Copy();
        if (data.reset_demo && demoMode) ResetDemo();
        if (data.health_score < 0)
            data.health_score = Mathf.Clamp(100 - 0.45f * data.damage_severity
                - 0.25f * MaintenanceAdvisor.Severity(data.vibration, vibrationLimits)
                - 0.15f * MaintenanceAdvisor.Severity(data.temperature, temperatureLimits)
                - 0.15f * MaintenanceAdvisor.Severity(data.motor_current, currentLimits), 0, 100);

        // Deterministic local thresholds prevent inconsistent upstream labels hiding a rupture.
        data.risk_level = MaintenanceAdvisor.Risk(data.health_score);
        data.current_condition = MaintenanceAdvisor.Condition(data.health_score);
        data.maintenance_status = MaintenanceAdvisor.MaintenanceStatus(data.health_score);
        data.inspection_status = source == "Keyboard demo" || data.data_source == "simulator"
            ? "Simulated inspection" : "Telemetry received; physical inspection unconfirmed";
        if (string.IsNullOrWhiteSpace(data.maintenance_action))
            data.maintenance_action = DamageReferenceProfile.Advice(data.damage_type, data.health_score);
        if (data.health_score < 10) data.maintenance_action = MaintenanceAdvisor.Action(data.health_score);
        data.damage_type = data.damage_type ?? "none";
        data.last_updated = DateTime.UtcNow.ToString("O");
        joints[data.joint_id[3] - '1'] = data;
        latest = data;
        SelectJoint(data.joint_id);
        inputSource = source == "UDP" ? data.data_source == "simulator" ? "UDP simulator" :
            data.data_source == "python_bridge" ? "Python bridge / external readings" : "UDP / unclassified source" : source;
        lastPacketTime = Time.unscaledTime;
        dataStatus = "Live";
        Refresh();
        if (record) store?.Record(data, this);
        return true;
    }

    public BeltHealthData WorstJoint()
    {
        BeltHealthData worst = joints[0];
        foreach (var joint in joints) if (joint.health_score < worst.health_score) worst = joint;
        return worst;
    }

    public void Refresh()
    {
        var worst = WorstJoint();
        rupture?.Apply(worst.health_score, latest == null ? 0 : latest.belt_speed);
        if (!HasFreshTelemetry) rupture?.StopMotion();
        visualizer?.Refresh(this);
    }

    public void ResetDemo()
    {
        if (!demoMode) return;
        Initialize();
        latest = null;
        lastPacketTime = -1;
        dataStatus = "Demo reset; awaiting telemetry";
        rupture?.ResetLatch();
        Refresh();
        store?.Save(this);
    }

    [ContextMenu("Acknowledge repaired conveyor - requires healthy zero-speed telemetry")]
    public void AcknowledgeMaintenance()
    {
        // This never fabricates new health readings and never sends a hardware start command.
        bool inspected = true;
        foreach (var joint in joints)
            if (!DateTime.TryParse(joint.last_updated, null, System.Globalization.DateTimeStyles.RoundtripKind, out var time)
                || (DateTime.UtcNow - time.ToUniversalTime()).TotalSeconds > 60) inspected = false;
        if (!HasFreshTelemetry || !inspected || latest == null || latest.belt_speed > 0 || WorstJoint().health_score < 80)
        { Debug.LogWarning("[Maintenance] Need fresh, zero-speed telemetry and healthy joint readings before clearing the latch."); return; }
        rupture.ResetLatch();
        rupture.operatorPaused = true;
        Refresh();
        store?.Save(this);
    }

    public void BeginExternalInput()
    {
        latest = null; lastPacketTime = -1;
        inputSource = "Awaiting UDP"; dataStatus = "Awaiting UDP";
        rupture.StopMotion();
    }

    void Update()
    {
        if (lastPacketTime >= 0 && Time.unscaledTime - lastPacketTime > staleAfterSeconds)
        {
            dataStatus = "STALE telemetry - simulated motion stopped";
            rupture?.StopMotion();
        }
    }
}
