using UnityEngine;
using UnityEngine.InputSystem;

// Camera presentation only. Never writes to health, motion, or emergency-stop state.
[DefaultExecutionOrder(500)]
[RequireComponent(typeof(Camera))]
public class IndustrialOperatorCamera : MonoBehaviour
{
    public enum View { Operator, Top, Joint, Free, Presentation }
    public JointHealthManager manager;
    public BeltLoopMotion loop;
    public Transform inspectionHead;
    public float width, length;
    [Min(.1f)] public float transitionSeconds = .8f;
    [Min(.1f)] public float orbitDegreesPerSecond = 3;
    public View currentView = View.Operator;

    Camera output;
    Transform viewsRoot;
    readonly Transform[] poses = new Transform[5];
    Vector3 center, side, velocity, freeVelocity, freePosition;
    float freeYaw, freePitch, orbitTime;
    bool configured;

    public void Configure(Vector3 conveyorCenter)
    {
        output = GetComponent<Camera>();
        center = conveyorCenter;
        width = Mathf.Max(width, .1f); length = Mathf.Max(length, width);
        // Retain the accessible side used by the existing scene camera.
        side = loop.across.normalized;
        if (Vector3.Dot(transform.position - center, side) < 0) side = -side;
        viewsRoot = new GameObject("Camera Views").transform;
        viewsRoot.SetParent(transform.parent, false);
        string[] names = { "Operator Camera", "Top Inspection Camera", "Joint Inspection Camera", "Free Camera", "Presentation Camera" };
        for (int i = 0; i < poses.Length; i++)
        {
            poses[i] = new GameObject(names[i]).transform;
            poses[i].SetParent(viewsRoot, false);
        }
        // Keep the scene's lens and initial pose; even the first view blends in.
        configured = true;
        Select(currentView);
    }

    public void Select(View view)
    {
        currentView = view;
        if (view == View.Free)
        {
            freePosition = transform.position;
            freeYaw = transform.eulerAngles.y;
            freePitch = Mathf.DeltaAngle(0, transform.eulerAngles.x);
            freeVelocity = Vector3.zero;
        }
        if (view == View.Presentation) orbitTime = 0;
    }

    void Update()
    {
        if (!configured) return;
        var keys = Keyboard.current;
        if (keys != null)
        {
            if (keys.f1Key.wasPressedThisFrame || keys.fKey.wasPressedThisFrame) Select(View.Operator);
            if (keys.f2Key.wasPressedThisFrame || keys.vKey.wasPressedThisFrame) Select(View.Top);
            if (keys.f3Key.wasPressedThisFrame || keys.cKey.wasPressedThisFrame) Select(View.Joint);
            if (keys.f4Key.wasPressedThisFrame) Select(View.Free);
            if (keys.f5Key.wasPressedThisFrame) Select(View.Presentation);
        }
        if (currentView == View.Free) MoveFreeCamera(keys);
        if (currentView == View.Presentation) orbitTime += Time.unscaledDeltaTime;
    }

    void LateUpdate()
    {
        if (!configured || !loop || !manager) return;
        UpdateViewPoses();
        var destination = poses[(int)currentView];
        // Unscaled time keeps inspection available while the machine is paused.
        float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
        transform.position = Vector3.SmoothDamp(transform.position, destination.position,
            ref velocity, transitionSeconds * .45f, Mathf.Infinity, dt);
        transform.rotation = Quaternion.Slerp(transform.rotation, destination.rotation,
            1 - Mathf.Exp(-6 * dt / transitionSeconds));
    }

    void UpdateViewPoses()
    {
        Bounds framing = loop.bounds;
        // Include a modest margin for the conveyor frame and camera gantry.
        framing.Expand(new Vector3(width * .4f, width * 2, width * .4f));
        if (inspectionHead) framing.Encapsulate(inspectionHead.position + Vector3.up * width * .2f);
        Vector3 aim = framing.center;
        float distance = FitDistance(framing);
        Vector3 along = loop.axis.normalized;

        // Elevated three-quarter overview exposes the green carrying surface.
        // Fit the actual corners rather than a large sphere, avoiding a distant side view.
        Vector3 overviewDirection = (side + along * .55f + Vector3.up * .8f).normalized;
        Quaternion overviewRotation = Quaternion.LookRotation(-overviewDirection, Vector3.up);
        Vector3 overviewAim = aim - (overviewRotation * Vector3.right) * length * .08f;
        Bounds overviewBounds = framing;
        overviewBounds.Encapsulate(loop.bounds.center - Vector3.up * width * 1.7f);
        float overviewDistance = FitOverview(overviewBounds, overviewAim, overviewRotation);
        poses[0].SetPositionAndRotation(overviewAim + overviewDirection * overviewDistance, overviewRotation);

        // Explicit screen-up direction avoids a rotation singularity directly overhead.
        Look(poses[1], aim + Vector3.up * distance, aim, along);

        string id = manager.DisplayJointId;
        int index = JointHealthManager.IsJointId(id) ? id[3] - '1' : 2;
        loop.JointPose(index, out var joint, out _, out _);
        // Stay above the frame when a joint passes through the return run.
        Vector3 inspectionPosition = joint + side * width * 2 + along * width * .5f;
        inspectionPosition.y = loop.bounds.max.y + width * 1.6f;
        Look(poses[2], inspectionPosition, joint, Vector3.up);

        poses[3].SetPositionAndRotation(freePosition, Quaternion.Euler(freePitch, freeYaw, 0));

        // A gentle orbit arc on the accessible side avoids flying behind the workshop wall.
        // Sine easing reverses direction without a jump at either end of the arc.
        float angle = 55 * Mathf.Sin(orbitTime * orbitDegreesPerSecond / 55);
        Vector3 radial = Quaternion.AngleAxis(angle, Vector3.up) * side;
        Vector3 direction = (radial + Vector3.up * .65f).normalized;
        Look(poses[4], aim + direction * distance, aim, Vector3.up);
    }

    float FitOverview(Bounds bounds, Vector3 aim, Quaternion rotation)
    {
        float vertical = Mathf.Tan(output.fieldOfView * Mathf.Deg2Rad * .5f) * .85f;
        float horizontal = vertical * Mathf.Max(output.aspect, .1f) * .9f;
        float distance = width;
        Quaternion inverse = Quaternion.Inverse(rotation);
        for (int i = 0; i < 8; i++)
        {
            Vector3 corner = bounds.center + Vector3.Scale(bounds.extents,
                new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
            Vector3 local = inverse * (corner - aim);
            // Two-argument overloads avoid a params-array allocation per corner/frame.
            distance = Mathf.Max(distance, Mathf.Max(Mathf.Abs(local.x) / horizontal - local.z,
                Mathf.Abs(local.y) / vertical - local.z));
        }
        return distance;
    }

    float FitDistance(Bounds bounds)
    {
        // Fit a sphere around the complete conveyor, including on narrow Game views.
        float vertical = output.fieldOfView * Mathf.Deg2Rad * .5f;
        float horizontal = Mathf.Atan(Mathf.Tan(vertical) * Mathf.Max(output.aspect, .1f));
        float limitingAngle = Mathf.Max(.01f, Mathf.Min(vertical, horizontal));
        return bounds.extents.magnitude / Mathf.Sin(limitingAngle) * 1.12f;
    }

    void MoveFreeCamera(Keyboard keys)
    {
        float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
        Vector3 input = Vector3.zero;
        var mouse = Mouse.current;
        // No cursor capture, head bob, sprint, or inertia-driven spinning.
        if (mouse != null && mouse.rightButton.isPressed)
        {
            Vector2 delta = mouse.delta.ReadValue();
            freeYaw += delta.x * .09f;
            freePitch = Mathf.Clamp(freePitch - delta.y * .09f, -85, 85);
            if (keys != null)
            {
                input.x = (keys.dKey.isPressed ? 1 : 0) - (keys.aKey.isPressed ? 1 : 0);
                input.z = (keys.wKey.isPressed ? 1 : 0) - (keys.sKey.isPressed ? 1 : 0);
                input.y = (keys.pageUpKey.isPressed ? 1 : 0) - (keys.pageDownKey.isPressed ? 1 : 0);
            }
        }
        Quaternion heading = Quaternion.Euler(0, freeYaw, 0);
        Vector3 desired = heading * Vector3.ClampMagnitude(input, 1) * length * .18f;
        freeVelocity = Vector3.Lerp(freeVelocity, desired, 1 - Mathf.Exp(-8 * dt));
        freePosition += freeVelocity * dt;
    }

    static void Look(Transform pose, Vector3 position, Vector3 target, Vector3 up)
        => pose.SetPositionAndRotation(position, Quaternion.LookRotation(target - position, up));

    void OnDestroy()
    {
        if (viewsRoot) Destroy(viewsRoot.gameObject);
    }
}
