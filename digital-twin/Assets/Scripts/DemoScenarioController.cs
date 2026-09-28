using UnityEngine;
using UnityEngine.InputSystem;

public class DemoScenarioController : MonoBehaviour
{
    [Header("Runtime references (assigned by scene setup)")]
    public JointHealthManager manager;
    public BeltHealthUDPReceiver receiver;
    [Header("Demonstration settings")]
    public bool startHealthy = true;
    [Tooltip("Illustrative health points lost per simulated second, not a measured physical failure rate.")]
    [Min(.1f)] public float healthLossPerSecond = 4;
    private readonly JointDegradationModel degradation = new JointDegradationModel();
    private bool keyboardMode;
    private bool localHeartbeat;
    private float encoder;
    public string affectedJoint = "J-03";
    private BeltHealthData scenario;
    private float nextHeartbeat;
    private float nextRecord;

    void Start() { if (startHealthy && manager.demoMode) { SeedHealthyJoints(); Select(1, false); } }
    void SeedHealthyJoints()
    {
        for (int i = 1; i <= 5; i++) manager.Apply(new BeltHealthData {
            joint_id = $"J-{i:00}", health_score = 100, belt_speed = .42f, motor_current = 1.2f,
            temperature = 30, vibration = 1, damage_type = "none", encoder_position = encoder,
            data_source = "simulator" }, "Keyboard demo");
    }

    public void Select(int stage, bool takeControl = true)
    {
        if (!manager.demoMode) return;
        stage = Mathf.Clamp(stage, 1, 5);
        keyboardMode = takeControl;
        localHeartbeat = true;
        if (receiver) { receiver.acceptPackets = !takeControl; receiver.ClearPending(); }
        // Start from the passport so switching targets cannot reset accumulated damage.
        scenario = manager.joints[affectedJoint[3] - '1'].Copy();
        scenario.belt_speed = .42f; scenario.encoder_position = encoder;
        scenario.data_source = "simulator";
        degradation.Request(stage);
        degradation.Step(scenario, 0, healthLossPerSecond);
        manager.Apply(scenario, "Keyboard demo");
    }

    void Update()
    {
        var keys = Keyboard.current;
        if (keys != null && keys.spaceKey.wasPressedThisFrame)
        { manager.rupture.operatorPaused = !manager.rupture.operatorPaused; manager.Refresh(); }
        if (keys != null && keys.eKey.wasPressedThisFrame)
        { manager.rupture.TriggerOperatorEmergency(); manager.Refresh(); manager.store?.Save(manager); }
        if (!manager.demoMode) return;
        if (keys != null)
        {
            if (keys.digit1Key.wasPressedThisFrame) Select(1);
            if (keys.digit2Key.wasPressedThisFrame) Select(2);
            if (keys.digit3Key.wasPressedThisFrame) Select(3);
            if (keys.digit4Key.wasPressedThisFrame) Select(4);
            if (keys.digit5Key.wasPressedThisFrame) Select(5);
            if (keys.rKey.wasPressedThisFrame) { manager.ResetDemo(); encoder = 0; SeedHealthyJoints(); Select(1); }
            if (keys.nKey.wasPressedThisFrame)
            {
                affectedJoint = $"J-{(affectedJoint[3] - '0') % 5 + 1:00}";
                manager.SelectJoint(affectedJoint);
                // Freeze the previous joint at its recorded condition. Only the
                // selected joint is driven by this simulator at any moment.
                if (localHeartbeat)
                {
                    scenario = manager.joints[affectedJoint[3] - '1'].Copy();
                    scenario.belt_speed = .42f; scenario.data_source = "simulator";
                    degradation.targetHealth = scenario.health_score;
                }
                Debug.Log("[Demo] Selected " + affectedJoint + "; press 1-5 to apply a condition.");
            }
            if (keys.uKey.wasPressedThisFrame)
            {
                keyboardMode = false;
                localHeartbeat = false;
                manager.BeginExternalInput();
                if (receiver) { receiver.ClearPending(); receiver.acceptPackets = true; }
            }
        }
        // A valid UDP reading owns the twin until a keyboard scenario is requested.
        if (!keyboardMode && manager.inputSource != "Keyboard demo") localHeartbeat = false;
        if (localHeartbeat && scenario != null && !manager.rupture.emergencyStop && !manager.rupture.operatorPaused)
        {
            encoder += scenario.belt_speed * Time.deltaTime;
            degradation.Step(scenario, Time.deltaTime, healthLossPerSecond);
        }
        if (localHeartbeat && scenario != null && (Time.unscaledTime >= nextHeartbeat ||
            (scenario.health_score == 0 && !manager.rupture.ruptureEvent)))
        {
            scenario.encoder_position = encoder;
            // Update presentation at 10 Hz; keep persistent history at 1 Hz.
            bool record = Time.unscaledTime >= nextRecord || (scenario.health_score == 0 && !manager.rupture.ruptureEvent);
            manager.Apply(scenario, "Keyboard demo", record);
            if (record) nextRecord = Time.unscaledTime + 1;
            nextHeartbeat = Time.unscaledTime + .1f;
        }
    }
}
