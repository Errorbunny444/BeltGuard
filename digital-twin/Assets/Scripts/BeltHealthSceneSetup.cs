using TMPro;
using System.Collections.Generic;
using UnityEngine;

// Attached to the existing scene. Builds only health overlays; the CAD frame stays intact.
[DefaultExecutionOrder(-500)]
public class BeltHealthSceneSetup : MonoBehaviour
{
    [Header("Existing CAD and motion references")]
    public BeltAnimation existingBelt;
    public ConveyorSpeedControl speedControl;
    public StartButton startButton;
    [Header("Data and demonstration")]
    public int udpPort = 5055;
    public bool demoMode = true;
    [Tooltip("Illustrative total belt loop length in metres; calibrate to hardware.")]
    public float loopLengthMeters = 10;
    [Header("Presentation and saved-scene compatibility")]
    public bool frameCameraForDemo = true;
    public bool industrialRubberAppearance = true;
    [Tooltip("Set by the editor builder after the inspection cell has been saved into the scene.")]
    public bool sceneEquipmentBaked;
    [HideInInspector] public int bakedStructureVersion;
    public bool showJointLabels = false;
    public float encoderZeroMeters;
    private JointHealthManager manager;
    private DamageVisualizer visualizer;
    private Bounds beltBounds;
    private bool alongX;
    private float length, width, height, travel;
    private string sampleStamp;
    private float sampleTime;
    private Vector3 axis;
    private Material rubber, amber, red, steel;
    private Material runtimeGreenSurface;
    private Texture2D rubberTexture;
    private Texture2D rubberNormal;
    private readonly List<Transform> labels = new List<Transform>();
    private readonly List<GameObject> healthLabels = new List<GameObject>();
    private BeltLoopMotion loop;

    void Awake()
    {
        if (!existingBelt) existingBelt = FindAnyObjectByType<BeltAnimation>();
        if (!speedControl) speedControl = FindAnyObjectByType<ConveyorSpeedControl>();
        if (!startButton) startButton = FindAnyObjectByType<StartButton>();
        if (!existingBelt || !speedControl || !startButton || !existingBelt.GetComponent<MeshFilter>())
        { Debug.LogError("[BeltHealth] Assign CAD BeltAnimation, SpeedControl and StartButton."); enabled = false; return; }

        // Legacy receivers must not bind ports or command the health demo.
        foreach (var component in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
        {
            if (component is UDPReceiver || component is BeltSyncReceiver || component is ProximitySyncReceiver ||
                component is BoltSpawner || component is BoltSorter || component is BeltSensor ||
                component is SimulationProximitySensor || component is InspectionManager ||
                component is ConveyorObjectMove || component is ConveyorPhysics) component.enabled = false;
        }
        startButton.sendHardwareCommands = false;
        startButton.allowKeyboardToggle = false;
        startButton.start = false;
        startButton.emergencyStop = false;
        beltBounds = existingBelt.GetComponent<Renderer>().bounds;
        alongX = beltBounds.size.x >= beltBounds.size.z;
        length = Mathf.Max(alongX ? beltBounds.size.x : beltBounds.size.z, 0.1f);
        width = Mathf.Max(alongX ? beltBounds.size.z : beltBounds.size.x, 0.05f);
        height = Mathf.Max(beltBounds.size.y, width * 0.12f);
        axis = alongX ? Vector3.right : Vector3.forward;
        if (frameCameraForDemo && Camera.main && !sceneEquipmentBaked)
        {
            // Open the old enclosed workcell for the wider overview camera.
            foreach (string wallName in new[] { "Wall", "LeftWall", "RightWall", "WallAccent", "Floor", "WorkcellPad" })
            {
                var wall = GameObject.Find(wallName);
                if (wall) wall.SetActive(false);
            }
            Vector3 across = alongX ? Vector3.forward : Vector3.right;
            Camera.main.transform.position = beltBounds.center + axis * length * 0.4f
                - across * length * 0.95f + Vector3.up * length * 0.8f;
            Camera.main.transform.LookAt(beltBounds.center);
            Camera.main.fieldOfView = 45;
            Debug.Log($"[BeltHealth] CAD bounds {beltBounds}; overview camera {Camera.main.transform.position}");
        }
        if (industrialRubberAppearance)
        {
            // The editor builder saves the lighting rig with the scene.  Only create it
            // at runtime for an older scene that has not been baked yet.
            if (!sceneEquipmentBaked && !GetComponent<ConveyorStudioLighting>()) gameObject.AddComponent<ConveyorStudioLighting>();
            // Runtime-only rubber texture: preserve the imported material asset and animation script.
            const int textureSize = 256;
            rubberTexture = new Texture2D(textureSize, textureSize);
            rubberNormal = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, true, true);
            rubberTexture.wrapMode = TextureWrapMode.Repeat;
            rubberNormal.wrapMode = TextureWrapMode.Repeat;
            for (int y = 0; y < textureSize; y++) for (int x = 0; x < textureSize; x++)
            {
                float grain = Mathf.PerlinNoise(x * .31f, y * .31f);
                float shade = .78f + grain * .16f + Mathf.PerlinNoise(x * .04f, y * .7f) * .06f;
                // Broad, subtle wear variation remains visible from the overview camera.
                // It is part of the green surface texture, not raised marker geometry.
                shade *= .78f + .22f * (.5f + .5f * Mathf.Sin(2 * Mathf.PI * x / textureSize));
                rubberTexture.SetPixel(x, y, new Color(shade, shade, shade));
                float dx = Mathf.PerlinNoise((x + 1) * .31f, y * .31f) - grain;
                float dy = Mathf.PerlinNoise(x * .31f, (y + 1) * .31f) - grain;
                var normal = new Vector3(-dx * .6f, -dy * .6f, 1).normalized;
                rubberNormal.SetPixel(x, y, new Color(normal.x * .5f + .5f, normal.y * .5f + .5f, normal.z * .5f + .5f, 1));
            }
            rubberTexture.Apply();
            rubberNormal.Apply();
            var surface = existingBelt.GetComponent<Renderer>().material;
            runtimeGreenSurface = surface; // Renderer.material produces a runtime material owned by this setup.
            // Green polyurethane is the normal running state on the physical prototype.
            // Damage and rupture states are layered separately by the health visualisers.
            surface.color = new Color(.035f, .28f, .095f);
            if (surface.HasProperty("_BaseColor")) surface.SetColor("_BaseColor", surface.color);
            if (surface.HasProperty("_Metallic")) surface.SetFloat("_Metallic", .02f);
            if (surface.HasProperty("_Smoothness")) surface.SetFloat("_Smoothness", .22f);
            surface.mainTexture = rubberTexture;
            if (surface.HasProperty("_BaseMap")) surface.SetTexture("_BaseMap", rubberTexture);
            surface.mainTextureScale = new Vector2(12, 4);
            if (surface.HasProperty("_BumpMap")) { surface.SetTexture("_BumpMap", rubberNormal); surface.EnableKeyword("_NORMALMAP"); }
        }
        rubber = Material("Belt rubber", new Color(0.055f, 0.065f, 0.075f));
        amber = Material("Caution amber", new Color(1, 0.5f, 0.02f));
        red = Material("Damage red", new Color(1, 0.04f, 0.02f));
        steel = Material("Sensor housing", new Color(0.16f, 0.3f, 0.36f));

        manager = gameObject.AddComponent<JointHealthManager>();
        manager.demoMode = demoMode;
        var rupture = gameObject.AddComponent<BeltRuptureStateManager>();
        visualizer = gameObject.AddComponent<DamageVisualizer>();
        manager.rupture = rupture; manager.visualizer = visualizer;
        rupture.startButton = startButton; rupture.speedControl = speedControl;
        loop = gameObject.AddComponent<BeltLoopMotion>();
        loop.manager = manager; loop.visualizer = visualizer; loop.bounds = beltBounds;
        loop.axis = axis; loop.across = alongX ? Vector3.forward : Vector3.right;
        loop.loopLengthMeters = loopLengthMeters; loop.encoderZeroMeters = encoderZeroMeters;
        rupture.speedMultiplier = loop.Perimeter / Mathf.Max(loopLengthMeters, .01f);
        speedControl.pulleyRadiusUnity = loop.Radius;
        existingBelt.visualMultiplier = 1 / loop.Perimeter;
        existingBelt.MapContinuousLoop(axis);
        existingBelt.enabled = true;
        existingBelt.script = startButton;
        existingBelt.spc = speedControl;
        existingBelt.gameObject.name = "Belt_Normal";
        rupture.beltNormal = existingBelt.gameObject;
        rupture.beltMinorCrack = BeltCopy("Belt_MinorCrack");
        rupture.beltCriticalCrack = BeltCopy("Belt_CriticalCrack");
        // Damage marks follow actual joint positions; the three intact states share the CAD mesh.
        var center = new Vector3(beltBounds.center.x, beltBounds.max.y - height * 0.08f, beltBounds.center.z);
        rupture.rupturedLeft = RupturePiece("Belt_Ruptured_Left");
        rupture.rupturedRight = RupturePiece("Belt_Ruptured_Right");
        var geometry = gameObject.AddComponent<BeltRuptureGeometry>();
        geometry.source = existingBelt.GetComponent<MeshFilter>(); geometry.manager = manager;
        geometry.loop = loop; geometry.states = rupture; rupture.geometry = geometry;
        rupture.separation = axis * width * .08f;
        rupture.CaptureHomes();

        for (int i = 0; i < 5; i++)
        {
            // Joint passports are logical locations on the belt.  Do not add raised
            // geometry here: a real splice is flush with the belt surface.
            var rail = center + axis * (i - 2) * length / 5 + Vector3.up * width * 0.7f
                + (alongX ? Vector3.forward : Vector3.right) * width * 0.7f;
            visualizer.jointLabels[i] = Label($"J-{i + 1:00}", transform, rail, width * 0.26f);
            visualizer.jointLabels[i].gameObject.SetActive(showJointLabels);
            healthLabels.Add(visualizer.jointLabels[i].gameObject);
        }
        visualizer.damageMarker = new GameObject("DamageMarker");
        visualizer.damageMarker.transform.SetParent(transform);
        var damage = gameObject.AddComponent<JointSurfaceDamage>();
        damage.manager = manager; damage.loop = loop; damage.beltWidth = width;
        var cell = GetComponentInChildren<IndustrialInspectionCell>(true);
        if (!cell)
        {
            var cellObject = new GameObject("IndustrialInspectionCell");
            cellObject.transform.SetParent(transform);
            cell = cellObject.AddComponent<IndustrialInspectionCell>();
            cell.Build(beltBounds, alongX);
        }
        cell.manager = manager;
        visualizer.warningLight = cell.warningLight;
        var alarm = Label("", transform, center + Vector3.up * width * 3.2f, width * 0.3f);
        alarm.name = "EmergencyAlarmText";
        visualizer.alarmText = alarm; visualizer.emergencyAlarmText = alarm.gameObject;

        if (Camera.main)
        {
            var camera = Camera.main.gameObject.AddComponent<IndustrialOperatorCamera>();
            camera.manager = manager; camera.loop = loop; camera.width = width; camera.length = length;
            camera.inspectionHead = cell.cameraHead; camera.Configure(center);
        }
        // Dashboard intentionally deferred. These inactive placeholders reserve the requested hierarchy.
        var hmi = new GameObject("HMI_Canvas", typeof(Canvas)); hmi.transform.SetParent(transform);
        foreach (var name in new[] { "SensorPanel", "JointHealthPanel", "MaintenancePanel" })
            new GameObject(name, typeof(RectTransform)).transform.SetParent(hmi.transform, false);
        hmi.SetActive(false);
        manager.Refresh();
        manager.store = gameObject.AddComponent<JointHealthStore>();
        manager.store.Configure(manager);
        var network = new GameObject("BeltHealthNetwork");
        network.transform.SetParent(transform); network.SetActive(false);
        var receiver = network.AddComponent<BeltHealthUDPReceiver>();
        receiver.port = udpPort; receiver.manager = manager; network.SetActive(true);
        var demo = gameObject.AddComponent<DemoScenarioController>(); demo.manager = manager; demo.receiver = receiver;
        var view = gameObject.AddComponent<MaintenanceTwinView>();
        view.manager = manager; view.beltLoop = loop; view.demo = demo;
        // The compact overlay replaces the large floating alarm text.
        alarm.gameObject.SetActive(false);
        visualizer.emergencyAlarmText = null;
        Debug.Log("[SIH26008] 1-5 conditions | N next joint | Space pause | E emergency | R demo reset | U UDP | F overview | C inspect joint | V inspection view | L labels. HMI deferred.");
    }


    GameObject BeltCopy(string name)
    {
        var source = existingBelt.gameObject;
        var copy = new GameObject(name);
        copy.transform.SetParent(source.transform.parent, false);
        copy.transform.localPosition = source.transform.localPosition;
        copy.transform.localRotation = source.transform.localRotation;
        copy.transform.localScale = source.transform.localScale;
        copy.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
        copy.AddComponent<MeshRenderer>().sharedMaterials = source.GetComponent<Renderer>().sharedMaterials;
        var animation = copy.AddComponent<BeltAnimation>();
        animation.script = startButton; animation.spc = speedControl;
        animation.visualMultiplier = existingBelt.visualMultiplier;
        animation.useCalibratedUV = existingBelt.useCalibratedUV;
        animation.uvPerWorldUnit = existingBelt.uvPerWorldUnit;
        animation.directionMultiplier = existingBelt.directionMultiplier;
        return copy;
    }

    GameObject RupturePiece(string name)
    {
        var piece = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        piece.transform.SetParent(transform, false);
        piece.GetComponent<Renderer>().sharedMaterial = rubber;
        return piece;
    }

    Vector3 Dimensions(float l, float h, float w) => alongX ? new Vector3(l, h, w) : new Vector3(w, h, l);
    GameObject Box(string name, Vector3 position, Vector3 scale, Material material)
    {
        var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name; box.transform.SetParent(transform, false);
        box.transform.position = position; box.transform.localScale = scale;
        box.GetComponent<Collider>().enabled = false;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }
    Material Material(string name, Color color)
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Standard");
        return new Material(shader) { name = name, color = color };
    }
    TextMeshPro Label(string text, Transform parent, Vector3 position, float size)
    {
        var label = new GameObject("Label").AddComponent<TextMeshPro>();
        label.transform.position = position;
        label.transform.localScale = Vector3.one * size;
        // Never parent text under scaled primitives/CAD meshes: billboarding would stretch glyphs.
        label.transform.SetParent(transform, true);
        labels.Add(label.transform);
        // Keep world size independent of the tiny marker mesh scale.
        label.fontSize = 7;
        label.text = text; label.alignment = TextAlignmentOptions.Center;
        label.rectTransform.sizeDelta = new Vector2(30, 5);
        label.color = Color.white;
        return label;
    }
    void LateUpdate()
    {
        if (!manager) return;
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        if (keyboard != null && keyboard.lKey.wasPressedThisFrame)
        {
            showJointLabels = !showJointLabels;
            foreach (var label in healthLabels) label.SetActive(showJointLabels);
        }
        var camera = Camera.main;
        if (camera) foreach (var label in labels) if (label) label.rotation = camera.transform.rotation;
    }

    void OnDestroy()
    {
        if (runtimeGreenSurface) Destroy(runtimeGreenSurface);
        if (rubber) Destroy(rubber); if (amber) Destroy(amber);
        if (red) Destroy(red); if (steel) Destroy(steel);
        if (rubberTexture) Destroy(rubberTexture);
        if (rubberNormal) Destroy(rubberNormal);
    }
}
