using UnityEngine;

[DefaultExecutionOrder(110)]
public class ConveyorPulley : MonoBehaviour
{
    [Header("References")]
    public GameObject FrontPulley;
    public GameObject BackPulley;
    public ConveyorSpeedControl spc;
    public StartButton script;

    private bool warned = false; // prevent log spam

    void Start()
    {
        // 🔥 Auto-find if not assigned
        if (spc == null)
            spc = Object.FindAnyObjectByType<ConveyorSpeedControl>();

        if (script == null)
            script = Object.FindAnyObjectByType<StartButton>();
    }

    void Update()
    {
        // 🔥 Safety check (only warn once)
        if (FrontPulley == null || BackPulley == null || spc == null || script == null)
        {
            if (!warned)
            {
                Debug.LogWarning("⚠ ConveyorPulley: Missing references!");
                warned = true;
            }
            return;
        }

        // 🔥 Rotate pulleys only when belt is ON
        if (script.start && !Mathf.Approximately(spc.ConvSpeed, 0f))
        {
            float rotation = spc.pulleyRadiusUnity > 0
                ? spc.FrameTravel / spc.pulleyRadiusUnity * Mathf.Rad2Deg
                : spc.PulleySpeed * Time.deltaTime;

            FrontPulley.transform.Rotate(Vector3.up * rotation, Space.Self);
            BackPulley.transform.Rotate(Vector3.up * rotation, Space.Self);
        }
    }
}
