using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Presentation only: reads the existing health state and never commands the conveyor.
[DefaultExecutionOrder(300)]
public class MaintenanceTwinView : MonoBehaviour
{
    public JointHealthManager manager;
    public BeltLoopMotion beltLoop;
    public DemoScenarioController demo;
    TMP_Text scenario, joint, health, risk, advice, adviceTitle;
    Image lamp, healthFill, accent;
    GameObject popup;
    readonly LineRenderer[] outlines = new LineRenderer[5];
    readonly Color[] displayed = new Color[5];
    Material lineMaterial;
    MaterialPropertyBlock highlightColor;
    // MaterialPropertyBlock creates native state and cannot run in a field initializer.
    void Awake() { highlightColor = new MaterialPropertyBlock(); }
    Canvas canvas;
    Light warning;
    float shownHealth = 100;
    string shownJoint;

    static readonly Color Green = new Color(.22f,.83f,.55f);
    static readonly Color Yellow = new Color(.91f,.81f,.3f);
    static readonly Color Amber = new Color(1,.52f,.16f);
    static readonly Color Red = new Color(1,.22f,.25f);
    static readonly Color Muted = new Color(.55f,.62f,.69f);

    public static Color HealthColor(float score)
    {
        if (score >= 72) return Color.Lerp(new Color(.62f,.82f,.42f), Green, Mathf.InverseLerp(72,100,score));
        if (score >= 52) return Color.Lerp(Yellow, new Color(.62f,.82f,.42f), Mathf.InverseLerp(52,72,score));
        if (score >= 25) return Color.Lerp(Amber, Yellow, Mathf.InverseLerp(25,52,score));
        return Color.Lerp(Red, Amber, Mathf.InverseLerp(0,25,score));
    }
    public static bool Observed(BeltHealthData data)
        => data != null && !string.IsNullOrEmpty(data.last_updated) && !data.last_updated.StartsWith("Never");

    void Start()
    {
        canvas = new GameObject("Maintenance overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler)).GetComponent<Canvas>();
        canvas.transform.SetParent(transform, false);
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 50;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1600,900); scaler.matchWidthOrHeight = .5f;
        var card = Panel("Current condition", canvas.transform, 24,24,320,236, new Color(.035f,.055f,.075f,.94f));
        Text(card.transform,"BELT HEALTH / SIMULATION",16,12,280,22,13,Muted);
        lamp = Panel("Warning and emergency light",card.transform,286,17,12,12,Green);
        scenario = Text(card.transform,"Awaiting data",16,39,288,34,23,Color.white);
        joint = Text(card.transform,"Current joint  —",16,86,288,25,18,Color.white);
        health = Text(card.transform,"Health  —",16,125,288,26,20,Color.white);
        Panel("Health track",card.transform,16,159,288,4,new Color(.15f,.19f,.23f));
        healthFill = Panel("Health value",card.transform,16,159,288,4,Green);
        risk = Text(card.transform,"Risk  —",16,180,288,30,18,Muted);
        var recommendation = Panel("Maintenance recommendation",canvas.transform,24,272,320,142,new Color(.035f,.055f,.075f,.96f));
        popup = recommendation.gameObject;
        accent = Panel("Risk accent",recommendation.transform,0,0,3,142,Green);
        adviceTitle = Text(recommendation.transform,"MAINTENANCE RECOMMENDATION",16,12,288,23,12,Muted);
        advice = Text(recommendation.transform,"Continue monitoring",16,41,288,90,17,Color.white);

        lineMaterial = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        lineMaterial.color = Color.white;
        for (int i = 0; i < 5; i++)
        {
            var line = new GameObject($"Joint highlight J-{i + 1:00}").AddComponent<LineRenderer>();
            line.transform.SetParent(transform,false); line.sharedMaterial = lineMaterial;
            line.useWorldSpace = true; line.loop = true; line.positionCount = 4;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false; line.numCornerVertices = 3;
            outlines[i] = line; displayed[i] = Muted;
        }
        warning = new GameObject("Joint warning light").AddComponent<Light>();
        warning.transform.SetParent(transform,false); warning.type = LightType.Point;
        warning.shadows = LightShadows.None; warning.range = beltLoop.bounds.size.z * 1.5f;
    }

    void LateUpdate()
    {
        if (!manager || !manager.rupture || !beltLoop || !canvas) return;
        var states = manager.rupture;
        string id = manager.DisplayJointId;
        int selected = JointHealthManager.IsJointId(id) ? id[3] - '1' : 2;
        var current = manager.joints[selected]; var worst = manager.WorstJoint();
        // Do not display the previous joint's score while changing selection.
        if (shownJoint != current.joint_id) { shownJoint = current.joint_id; shownHealth = current.health_score; }
        bool fresh = manager.HasFreshTelemetry;
        bool known = fresh && Observed(current);
        bool emergency = states.emergencyStop;
        Color target = known ? HealthColor(current.health_score) : Muted;
        scenario.text = states.ruptureEvent ? states.IsRuptureTransitioning ? "Rupture initiating / stopped" : "Rupture / stopped" : emergency ? "Emergency stop" :
            !fresh ? manager.latest == null ? "Awaiting data" : "Data stale / stopped" :
            states.operatorPaused ? "Paused" : known ? Scenario(current.health_score) : "Awaiting inspection";
        joint.text = "Current joint  " + current.joint_id;
        shownHealth = emergency ? current.health_score : Mathf.Lerp(shownHealth, current.health_score, 1 - Mathf.Exp(-12 * Time.unscaledDeltaTime));
        health.text = known ? $"Health  {shownHealth:0} / 100" : "Health  —";
        health.color = target; healthFill.color = target;
        healthFill.rectTransform.sizeDelta = new Vector2(known ? 288 * shownHealth / 100 : 0,4);
        risk.text = known ? "Risk  " + MaintenanceAdvisor.Risk(current.health_score).ToUpperInvariant() : "Risk  UNKNOWN";
        risk.color = target;

        // Escalation is based on the worst joint even while the operator views another one.
        bool warn = fresh && worst.health_score < 60;
        float warningStrength = Mathf.InverseLerp(60,10,worst.health_score);
        float pulse = .3f + .7f * (.5f + .5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2));
        Color alert = emergency ? Red : !fresh ? Muted : HealthColor(worst.health_score);
        lamp.color = emergency ? Color.Lerp(Red * .35f,Red,pulse) :
            Color.Lerp(alert, Color.Lerp(alert * .35f,alert,pulse), warningStrength);
        lamp.color = new Color(lamp.color.r,lamp.color.g,lamp.color.b,1);
        var recommendation = worst.health_score < current.health_score ? worst : current;
        adviceTitle.text = "MAINTENANCE  /  " + recommendation.joint_id;
        advice.text = emergency ? states.ruptureEvent ? "Stop latched. Isolate conveyor and inspect the failed splice before repair."
            : "Emergency stop latched. Investigate the cause before restarting."
            : !fresh ? "Waiting for fresh readings. Historical health must not be treated as a current inspection."
            : Observed(recommendation) ? recommendation.maintenance_action : "Awaiting joint inspection.";
        accent.color = alert;
        // A small contextual popup stays readable; no repeated opening on each UDP packet.
        popup.SetActive(true);

        float width = beltLoop.axis.x != 0 ? beltLoop.bounds.size.z : beltLoop.bounds.size.x;
        for (int i = 0; i < 5; i++)
        {
            var data = manager.joints[i]; var line = outlines[i];
            bool inspected = fresh && Observed(data);
            displayed[i] = Color.Lerp(displayed[i], inspected ? HealthColor(data.health_score) : Muted, 1 - Mathf.Exp(-8 * Time.unscaledDeltaTime));
            beltLoop.JointPose(i,out var p,out var normal,out var tangent);
            // Thin surface outlines replace bulky geometry. Hidden on the underside.
            line.enabled = !states.ruptureEvent && normal.y > .4f && i == selected;
            line.startWidth = line.endWidth = width * (i == selected ? .007f : .003f);
            line.startColor = line.endColor = displayed[i];
            // URP Unlit does not reliably use LineRenderer vertex colours.
            highlightColor.SetColor("_BaseColor", displayed[i]);
            line.SetPropertyBlock(highlightColor);
            p += normal * width * .009f;
            Vector3 a = beltLoop.across * width * .43f, b = tangent * width * .055f;
            line.SetPosition(0,p-a-b); line.SetPosition(1,p+a-b);
            line.SetPosition(2,p+a+b); line.SetPosition(3,p-a+b);
        }
        int alertJoint = worst.joint_id[3] - '1';
        beltLoop.JointPose(alertJoint,out var location,out var up,out _);
        warning.transform.position = location + up * width * .25f;
        warning.enabled = emergency || warn; warning.color = emergency ? Red : alert;
        warning.intensity = (emergency ? 2 : warningStrength) * pulse;
    }

    static string Scenario(float value) => MaintenanceAdvisor.Condition(value);
    static Image Panel(string name,Transform parent,float x,float y,float w,float h,Color color)
    {
        var image = new GameObject(name,typeof(RectTransform),typeof(CanvasRenderer),typeof(Image)).GetComponent<Image>();
        Place(image.rectTransform,parent,x,y,w,h); image.color = color; image.raycastTarget = false; return image;
    }
    static TMP_Text Text(Transform parent,string value,float x,float y,float w,float h,float size,Color color)
    {
        var text = new GameObject(value,typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
        Place(text.rectTransform,parent,x,y,w,h); text.text = value; text.fontSize = size;
        text.color = color; text.raycastTarget = false; text.richText = false;
        text.enableAutoSizing = true; text.fontSizeMin = size - 3; text.fontSizeMax = size;
        return text;
    }
    static void Place(RectTransform rect,Transform parent,float x,float y,float w,float h)
    {
        rect.SetParent(parent,false); rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0,1);
        rect.anchoredPosition = new Vector2(x,-y); rect.sizeDelta = new Vector2(w,h);
    }
    void OnDestroy() { if (lineMaterial) Destroy(lineMaterial); }
}
