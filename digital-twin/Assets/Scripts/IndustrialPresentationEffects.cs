using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

// Optional cosmetic layer. Attach beside BeltHealthSceneSetup; no state is written back.
[DefaultExecutionOrder(700)]
[DisallowMultipleComponent]
public class IndustrialPresentationEffects : MonoBehaviour
{
    [Header("Optional assignments")]
    public Renderer[] metalSurfaces = new Renderer[0];
    [Tooltip("A stationary mesh-only motor housing, never a shaft, collider or animated parent.")]
    public Transform motorVisual;
    public bool subtleDust;
    [Range(0, .001f)] public float vibrationFractionOfBeltWidth = .00015f;

    JointHealthManager manager;
    BeltLoopMotion loop;
    ConveyorSpeedControl speed;
    Transform root;
    Renderer beacon;
    Image lamp;
    CanvasGroup card;
    RectTransform recommendation;
    TMP_Text message;
    ParticleSystem dust;
    Material effectMaterial, dustMaterial;
    MaterialPropertyBlock block;
    // Initialize before OnEnable/Start; Unity objects cannot be created in constructors.
    void Awake() { block = new MaterialPropertyBlock(); }
    readonly List<Surface> surfaces = new List<Surface>();
    readonly List<Renderer> accents = new List<Renderer>();
    readonly Dictionary<Renderer, int> accentSlots = new Dictionary<Renderer, int>();
    bool ownsCard;
    float originalCardAlpha = 1;
    Vector3 motorHome;
    Vector2 recommendationHome;
    string previousMessage;
    float reveal, changeAge = 1, vibration;
    bool initialized, motorAllowed;
    float width;

    class Surface
    {
        public Renderer renderer;
        public MaterialPropertyBlock original;
        public bool receive;
    }

    void Start() { Build(); }
    void OnEnable() { if (initialized) Build(); }

    void Build()
    {
        dust = null; lamp = null; message = null; recommendation = null;
        manager = GetComponent<JointHealthManager>(); loop = GetComponent<BeltLoopMotion>();
        if (!manager || !loop || !manager.rupture) return;
        initialized = true;
        speed = manager.rupture.speedControl;
        width = loop.axis.x != 0 ? loop.bounds.size.z : loop.bounds.size.x;
        root = new GameObject("Industrial presentation effects").transform;
        root.SetParent(transform, false);
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) { Debug.LogWarning("Presentation effects require the existing URP shaders."); enabled = false; return; }
        effectMaterial = new Material(shader) { name = "Runtime beacon lens" };
        // Mesh only: no collider, new realtime light, or bloom dependency.
        var lens = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        var collider = lens.GetComponent<Collider>(); collider.enabled = false; Destroy(collider);
        lens.name = "Gantry status beacon"; lens.transform.SetParent(root, false);
        var cell = GetComponentInChildren<IndustrialInspectionCell>(true);
        Vector3 mount = cell && cell.cameraHead ? cell.cameraHead.position : loop.bounds.center;
        lens.transform.position = mount + Vector3.up * width * .34f;
        // Convert dimensions to world scale, independent of the CAD parent's scale.
        lens.transform.SetParent(null, true);
        lens.transform.localScale = new Vector3(width * .045f, width * .04f, width * .045f);
        lens.transform.SetParent(root, true);
        beacon = lens.GetComponent<Renderer>(); beacon.sharedMaterial = effectMaterial;
        beacon.shadowCastingMode = ShadowCastingMode.Off; beacon.receiveShadows = false;

        var overlay = transform.Find("Maintenance overlay");
        if (overlay)
        {
            var condition = overlay.Find("Current condition");
            if (condition)
            {
                card = condition.GetComponent<CanvasGroup>();
                if (!card) { card = condition.gameObject.AddComponent<CanvasGroup>(); ownsCard = true; }
                originalCardAlpha = card.alpha;
                var indicator = condition.Find("Warning and emergency light");
                if (indicator) lamp = indicator.GetComponent<Image>();
            }
            recommendation = overlay.Find("Maintenance recommendation") as RectTransform;
            if (recommendation)
            {
                recommendationHome = recommendation.anchoredPosition;
                var texts = recommendation.GetComponentsInChildren<TMP_Text>();
                if (texts.Length > 1) message = texts[1];
            }
        }
        // Property blocks retain animated UV offsets and do not alter material assets.
        AddSurface(manager.rupture.beltNormal, .02f, .25f);
        AddSurface(manager.rupture.beltMinorCrack, .02f, .25f);
        AddSurface(manager.rupture.beltCriticalCrack, .02f, .25f);
        foreach (var metal in metalSurfaces) if (metal) Finish(metal, .78f, .48f);
        accents.Clear();
        accentSlots.Clear();
        foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            if (renderer.name.StartsWith("Joint highlight J-") || renderer.name.StartsWith("Physical crack J-"))
            {
                accents.Add(renderer);
                // Cache once; sharedMaterials returns an array and should not be read per frame.
                accentSlots[renderer] = renderer is LineRenderer ? 0 : renderer.sharedMaterials.Length;
            }
        motorAllowed = motorVisual && motorVisual.childCount == 0 && motorVisual.GetComponent<MeshRenderer>() &&
            !motorVisual.GetComponentInParent<Rigidbody>() && !motorVisual.GetComponent<Collider>() &&
            !motorVisual.GetComponentInParent<Animator>() && !motorVisual.GetComponentInParent<Animation>() &&
            !motorVisual.GetComponentInParent<ConveyorPulley>() && !motorVisual.GetComponentInParent<BeltAnimation>() &&
            motorVisual.GetComponents<MonoBehaviour>().Length == 0;
        if (motorVisual && !motorAllowed) Debug.LogWarning("Motor vibration skipped: assign an unanimated, mesh-only housing.");
        if (motorAllowed) motorHome = motorVisual.localPosition;
        if (subtleDust) CreateDust(shader);
        reveal = 0; previousMessage = null;
    }

    void AddSurface(GameObject item, float metallic, float smoothness)
    { if (item) { var renderer = item.GetComponent<Renderer>(); if (renderer) Finish(renderer, metallic, smoothness); } }

    void Finish(Renderer renderer, float metallic, float smoothness)
    {
        if (surfaces.Exists(s => s.renderer == renderer)) return;
        var original = new MaterialPropertyBlock(); renderer.GetPropertyBlock(original);
        surfaces.Add(new Surface { renderer = renderer, original = original, receive = renderer.receiveShadows });
        renderer.GetPropertyBlock(block);
        block.SetFloat("_Metallic", metallic); block.SetFloat("_Smoothness", smoothness);
        renderer.SetPropertyBlock(block); renderer.receiveShadows = true;
    }

    void LateUpdate()
    {
        if (!root || !manager || !manager.rupture) return;
        float dt = Mathf.Min(Time.unscaledDeltaTime, .05f);
        bool emergency = manager.rupture.emergencyStop;
        bool running = manager.HasFreshTelemetry && !emergency && !manager.rupture.operatorPaused && speed && Mathf.Abs(speed.ConvSpeed) > .001f;
        // Use the existing indicator colour and pulse, not a second alarm oscillator.
        Color signal = lamp ? lamp.color : new Color(.2f,.35f,.25f);
        effectMaterial.SetColor("_BaseColor", signal);
        float pulse = .3f + .7f * (.5f + .5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2));
        foreach (var accent in accents)
        {
            if (!accent || !accent.enabled) continue;
            int slots = accentSlots[accent];
            if (slots == 0) PulseAccent(accent, -1, 1 + .08f * pulse);
            else for (int slot = 0; slot < slots; slot++) PulseAccent(accent, slot, 1 + .025f * pulse);
        }
        reveal = Mathf.MoveTowards(reveal, 1, dt * 4);
        if (card) card.alpha = emergency ? 1 : Mathf.Lerp(.85f, 1, reveal);
        if (message && message.text != previousMessage) { previousMessage = message.text; changeAge = 0; }
        changeAge += dt;
        // Text never disappears. Only a two-pixel settle on a changed recommendation.
        if (recommendation) recommendation.anchoredPosition = recommendationHome + Vector2.down * (emergency ? 0 : 2 * Mathf.Exp(-12 * changeAge));
        vibration = Mathf.Lerp(vibration, running ? 1 : 0, 1 - Mathf.Exp(-12 * dt));
        if (motorAllowed && motorVisual)
        {
            Vector3 world = Vector3.up * (Mathf.Sin(Time.unscaledTime * 43) * width * vibrationFractionOfBeltWidth * vibration);
            motorVisual.localPosition = motorHome + (motorVisual.parent ? motorVisual.parent.InverseTransformVector(world) : world);
        }
        if (dust)
        {
            var emission = dust.emission; emission.enabled = running;
        }
    }

    void PulseAccent(Renderer renderer, int slot, float gain)
    {
        // The producer updates its base tint earlier in LateUpdate. Match its property
        // block scope: outlines use one block; layered cracks use one per material.
        if (slot < 0) renderer.GetPropertyBlock(block); else renderer.GetPropertyBlock(block, slot);
        Color color = block.GetColor("_BaseColor");
        block.SetColor("_BaseColor", new Color(color.r * gain, color.g * gain, color.b * gain, color.a));
        if (slot < 0) renderer.SetPropertyBlock(block); else renderer.SetPropertyBlock(block, slot);
    }

    void CreateDust(Shader shader)
    {
        var item = new GameObject("Optional inspection dust"); item.transform.SetParent(root, false);
        item.transform.position = loop.bounds.center + Vector3.up * width * .2f;
        dust = item.AddComponent<ParticleSystem>(); dust.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = dust.main; main.maxParticles = 24; main.startLifetime = 5; main.startSpeed = width * .012f;
        main.startSize = width * .002f; main.startColor = new Color(.55f,.52f,.45f,.1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World; main.playOnAwake = false;
        var emission = dust.emission; emission.rateOverTime = 2;
        var shape = dust.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(width * .4f, width * .04f, width * .4f);
        // Uniform translucent tint also works with URP Unlit (no vertex-colour assumption).
        dustMaterial = new Material(shader) { name = "Runtime sparse dust" };
        dustMaterial.SetColor("_BaseColor", new Color(.55f,.52f,.45f,.08f));
        dustMaterial.SetFloat("_Surface", 1); dustMaterial.SetFloat("_ZWrite", 0);
        dustMaterial.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha); dustMaterial.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        dustMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT"); dustMaterial.renderQueue = 3000;
        var renderer = dust.GetComponent<ParticleSystemRenderer>(); renderer.sharedMaterial = dustMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off; renderer.receiveShadows = false;
        dust.Play();
    }

    void OnDisable()
    {
        foreach (var surface in surfaces) if (surface.renderer)
        { surface.renderer.SetPropertyBlock(surface.original); surface.renderer.receiveShadows = surface.receive; }
        surfaces.Clear(); accents.Clear(); accentSlots.Clear();
        if (motorAllowed && motorVisual) motorVisual.localPosition = motorHome;
        if (recommendation) recommendation.anchoredPosition = recommendationHome;
        // Keep an owned group for re-enable; never delete a group supplied by the scene.
        if (card) card.alpha = originalCardAlpha;
        if (root) { root.gameObject.SetActive(false); Destroy(root.gameObject); }
        if (effectMaterial) Destroy(effectMaterial);
        if (dustMaterial) Destroy(dustMaterial);
    }
    void OnDestroy() { if (ownsCard && card) Destroy(card); }
}
