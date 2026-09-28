using UnityEngine;
using UnityEngine.InputSystem;

public class ConveyorInspectionCamera : MonoBehaviour
{
    public JointHealthManager manager;
    public BeltLoopMotion loop;
    public float width, length;
    public Transform inspectionHead;
    private Vector3 overview, target;
    private float distance, yaw = 130, pitch = 30;
    private bool followingJoint;

    public void Configure(Vector3 center)
    {
        overview = center; target = center;
        distance = length * 1.12f;
        // View across the long side rather than down the belt.
        yaw = loop.axis.x != 0 ? 25 : -65;
        UpdatePose();
    }
    void Update()
    {
        var keys = Keyboard.current;
        if (keys != null)
        {
            if (keys.fKey.wasPressedThisFrame) { followingJoint = false; target = overview; distance = length * 1.12f; pitch = 30; }
            if (keys.cKey.wasPressedThisFrame) FocusWorstJoint();
            if (keys.vKey.wasPressedThisFrame && inspectionHead)
            { followingJoint = false; target = loop.bounds.center; distance = width * 1.7f; pitch = 85; }
        }
        if (followingJoint)
        {
            loop.JointPose(manager.WorstJoint().joint_id[3] - '1', out target, out _, out _);
        }
        var mouse = Mouse.current;
        if (mouse != null)
        {
            if (mouse.rightButton.isPressed)
            { var delta = mouse.delta.ReadValue(); yaw += delta.x * .15f; pitch = Mathf.Clamp(pitch - delta.y * .15f, 5, 88); }
            distance = Mathf.Clamp(distance * Mathf.Exp(-mouse.scroll.ReadValue().y * .001f), width, length * 2);
        }
        UpdatePose();
    }
    public void FocusWorstJoint()
    {
        followingJoint = true; distance = width * 2.5f; pitch = 35;
        loop.JointPose(manager.WorstJoint().joint_id[3] - '1', out var point, out _, out _);
        bool downstream = Vector3.Dot(point - loop.bounds.center, loop.axis) >= 0;
        // Inspect from the nearer belt end, keeping the optical head behind the target.
        yaw = loop.axis.x != 0 ? (downstream ? -90 : 90) : (downstream ? 180 : 0);
    }
    void UpdatePose()
    {
        var direction = Quaternion.Euler(pitch, yaw, 0) * Vector3.back;
        transform.position = target + direction * distance;
        transform.LookAt(target);
    }
}
