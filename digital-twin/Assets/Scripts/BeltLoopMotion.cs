using UnityEngine;

// Stadium-shaped closed loop: straight carrying/return runs and continuous pulley wraps.
public class BeltLoopMotion : MonoBehaviour
{
    public JointHealthManager manager;
    public DamageVisualizer visualizer;
    public Bounds bounds;
    public float loopLengthMeters = 10;
    public float encoderZeroMeters;
    public float travelMeters;
    public Vector3 axis = Vector3.right;
    public Vector3 across = Vector3.forward;
    public float Radius => Mathf.Max(bounds.extents.y, .01f);
    public float Straight => Mathf.Max((axis.x != 0 ? bounds.size.x : bounds.size.z) - 2 * Radius, .01f);
    public float Perimeter => 2 * Straight + 2 * Mathf.PI * Radius;
    private string sampleStamp;
    private float sampleTime;

    public void JointPose(int joint, out Vector3 position, out Vector3 normal, out Vector3 tangent)
    {
        // Encoder zero locates J-03 halfway along the visible inspection run.
        float phase = Straight * .5f / Perimeter + (joint - 2) / 5f
            + (travelMeters - encoderZeroMeters) / Mathf.Max(loopLengthMeters, .01f);
        Evaluate(phase, out position, out normal, out tangent);
    }

    public void Evaluate(float phase, out Vector3 position, out Vector3 normal, out Vector3 tangent)
    {
        float distance = Mathf.Repeat(phase, 1) * Perimeter;
        float straight = Straight, radius = Radius;
        float x, y;
        if (distance < straight)
        { x = -straight * .5f + distance; y = radius; normal = Vector3.up; tangent = axis; }
        else if (distance < straight + Mathf.PI * radius)
        {
            float angle = Mathf.PI * .5f - (distance - straight) / radius;
            x = straight * .5f + radius * Mathf.Cos(angle); y = radius * Mathf.Sin(angle);
            normal = axis * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle);
            tangent = axis * Mathf.Sin(angle) - Vector3.up * Mathf.Cos(angle);
        }
        else if (distance < 2 * straight + Mathf.PI * radius)
        {
            x = straight * .5f - (distance - straight - Mathf.PI * radius); y = -radius;
            normal = Vector3.down; tangent = -axis;
        }
        else
        {
            float angle = -Mathf.PI * .5f - (distance - 2 * straight - Mathf.PI * radius) / radius;
            x = -straight * .5f + radius * Mathf.Cos(angle); y = radius * Mathf.Sin(angle);
            normal = axis * Mathf.Cos(angle) + Vector3.up * Mathf.Sin(angle);
            tangent = axis * Mathf.Sin(angle) - Vector3.up * Mathf.Cos(angle);
        }
        position = bounds.center + axis * x + Vector3.up * y;
    }

    void LateUpdate()
    {
        if (!manager || !visualizer) return;
        var data = manager.latest;
        if (data != null && manager.dataStatus == "Live")
        {
            if (data.last_updated != sampleStamp)
            { sampleStamp = data.last_updated; sampleTime = Time.unscaledTime; }
            if (!manager.rupture.emergencyStop && !manager.rupture.operatorPaused)
                travelMeters = data.encoder_position + data.belt_speed * Mathf.Min(Time.unscaledTime - sampleTime, manager.staleAfterSeconds);
        }
        for (int i = 0; i < 5; i++)
        {
            JointPose(i, out var position, out var normal, out _);
            var marker = visualizer.jointMarkers[i];
            if (!marker) continue;
            marker.SetPositionAndRotation(position + normal * .015f, Quaternion.LookRotation(across, normal));
        }
    }
}
