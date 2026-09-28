using UnityEngine;

// Runs before the existing animation components, enforcing the stop interlock every frame.
[DefaultExecutionOrder(-100)]
public class BeltRuptureStateManager : MonoBehaviour
{
    public GameObject beltNormal, beltMinorCrack, beltCriticalCrack;
    public GameObject rupturedLeft, rupturedRight;
    public StartButton startButton;
    public ConveyorSpeedControl speedControl;
    [Tooltip("CAD animation units per m/s. Visual calibration only.")]
    public float speedMultiplier = 12;
    public Vector3 separation = new Vector3(0.1f, 0, 0);
    [Header("Visual failure sequence")]
    [Tooltip("The safety stop is immediate. This only controls how the final crack turns into separated belt pieces.")]
    [Min(.1f)] public float ruptureRevealSeconds = .8f;
    [Range(0, .9f)] public float criticalCrackHold = .28f;
    public bool emergencyStop;
    public bool ruptureEvent;
    public string ruptureJointId;
    public bool operatorPaused;
    public BeltRuptureGeometry geometry;
    public string systemState = "Healthy";
    [HideInInspector] public float ruptureVisualProgress = 1;
    public bool IsRuptureTransitioning => ruptureEvent && ruptureVisualProgress < 1;
    private Vector3 leftHome, rightHome;
    private bool captured;
    private float ruptureStartedAt = -1;
    private float lastAppliedHealth = 100;

    public void CaptureHomes()
    {
        if (captured) return;
        if (rupturedLeft) leftHome = rupturedLeft.transform.localPosition;
        if (rupturedRight) rightHome = rupturedRight.transform.localPosition;
        captured = true;
    }

    public void Apply(float health, float speed)
    {
        CaptureHomes();
        lastAppliedHealth = health;
        if (health < 10 && !ruptureEvent)
        {
            if (geometry)
            { ruptureJointId = geometry.manager.WorstJoint().joint_id; geometry.Build(ruptureJointId); }
            emergencyStop = true; ruptureEvent = true;
            ruptureStartedAt = Time.unscaledTime;
            ruptureVisualProgress = 0;
        }
        // A stored rupture has no running transition to resume; render its final state.
        else if (ruptureEvent && ruptureStartedAt < 0) ruptureVisualProgress = 1;
        systemState = ruptureEvent ? "Rupture / emergency stop" : emergencyStop ? "Operator emergency stop" :
            operatorPaused ? "Operator paused" : MaintenanceAdvisor.Risk(health);
        UpdateVisualState();
        if (startButton) startButton.emergencyStop = emergencyStop;
        if (emergencyStop || operatorPaused) StopMotion();
        else
        {
            if (speedControl) speedControl.ConvSpeed = speed * speedMultiplier;
            if (startButton) startButton.SetRunning(speed > 0);
        }
    }

    void UpdateVisualState()
    {
        bool revealPieces = ruptureEvent && ruptureVisualProgress >= criticalCrackHold;
        Set(beltNormal, !ruptureEvent && lastAppliedHealth >= 60);
        Set(beltMinorCrack, !ruptureEvent && lastAppliedHealth >= 40 && lastAppliedHealth < 60);
        // Keep the critical surface and grown crack on screen briefly before it opens.
        Set(beltCriticalCrack, !ruptureEvent ? lastAppliedHealth < 40 : !revealPieces);
        Set(rupturedLeft, revealPieces);
        Set(rupturedRight, revealPieces);
        float separationProgress = ruptureEvent
            ? Mathf.SmoothStep(0, 1, Mathf.InverseLerp(criticalCrackHold, 1, ruptureVisualProgress)) : 0;
        if (rupturedLeft) rupturedLeft.transform.localPosition = leftHome - separation * separationProgress;
        if (rupturedRight) rupturedRight.transform.localPosition = rightHome + separation * separationProgress;
    }

    public void StopMotion()
    {
        if (startButton) startButton.SetRunning(false);
        if (speedControl) speedControl.ConvSpeed = 0;
    }

    public void ResetLatch()
    {
        emergencyStop = false;
        ruptureEvent = false;
        ruptureJointId = null;
        operatorPaused = false;
        ruptureStartedAt = -1;
        ruptureVisualProgress = 1;
        lastAppliedHealth = 100;
        UpdateVisualState();
        if (startButton) startButton.emergencyStop = false;
    }

    public void TriggerOperatorEmergency()
    {
        emergencyStop = true;
        if (startButton) startButton.emergencyStop = true;
        StopMotion();
    }
    void Update()
    {
        if (ruptureEvent && ruptureVisualProgress < 1)
        {
            ruptureVisualProgress = Mathf.Clamp01((Time.unscaledTime - ruptureStartedAt) / ruptureRevealSeconds);
            UpdateVisualState();
        }
        if (emergencyStop || operatorPaused) StopMotion();
    }
    static void Set(GameObject target, bool active) { if (target) target.SetActive(active); }
}
