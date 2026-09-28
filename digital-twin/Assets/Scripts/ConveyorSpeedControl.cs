using UnityEngine;

[DefaultExecutionOrder(100)]
public class ConveyorSpeedControl : MonoBehaviour
{
    public double TravelDistance { get; private set; }
    public float FrameTravel { get; private set; }
    [Header("References")]
    public StartButton startButton;

    [Header("Speed Settings")]
    [Tooltip("Linear belt speed used by the digital twin. Positive = forward, negative = reverse.")]
    public float ConvSpeed = 0f;
    [Tooltip("Positive value enables radius-based angular velocity; zero preserves legacy calibration.")]
    public float pulleyRadiusUnity;

    [HideInInspector] public float PulleySpeed;
    [HideInInspector] public float BeltSpeed;

    private const float PulleyFactor = 32.25f;

    void Update()
    {
        FrameTravel = 0;
        if (startButton == null || !startButton.start || Mathf.Approximately(ConvSpeed, 0f))
        {
            PulleySpeed = 0f;
            BeltSpeed = 0f;
            return;
        }

        PulleySpeed = pulleyRadiusUnity > 0 ? ConvSpeed / pulleyRadiusUnity * Mathf.Rad2Deg : PulleyFactor * ConvSpeed;
        FrameTravel = ConvSpeed * Time.deltaTime;
        TravelDistance += FrameTravel;
        BeltSpeed = -PulleySpeed / 100.0f;
    }
}
