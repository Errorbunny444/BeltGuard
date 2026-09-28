using UnityEngine;
using TMPro;
using BeltGuard;

// Dimensioned equipment built around the existing CAD, in a local frame where belt width = 1.
// This represents an unloaded belt inspection test rig, not a full-size ore handling installation.
public class IndustrialInspectionCell : MonoBehaviour
{
    public JointHealthManager manager;
    public Transform cameraHead;
    public Transform statusLamp;
    public GameObject warningLight;
    private MaterialPropertyBlock lampColor;
    private Material metal, dark, yellow, black, lens, white, concrete;

    public void Build(Bounds belt, bool alongX)
    {
        float width = alongX ? belt.size.z : belt.size.x;
        float length = (alongX ? belt.size.x : belt.size.z) / width;
        // Model coordinates: x across the conveyor; z in the feed direction; y up.
        transform.position = new Vector3(belt.center.x, belt.max.y, belt.center.z);
        transform.rotation = Quaternion.LookRotation(alongX ? Vector3.right : Vector3.forward, Vector3.up);
        transform.localScale = Vector3.one * width;
        metal = IndustrialGeometry.Finish("Brushed enclosure aluminium", new Color(.4f, .45f, .49f), .75f, .4f);
        dark = IndustrialGeometry.Finish("Graphite powder-coated steel", new Color(.045f, .065f, .08f), .3f, .32f);
        yellow = IndustrialGeometry.Finish("Safety yellow coating", new Color(.86f, .5f, .035f), .15f, .3f);
        black = IndustrialGeometry.Finish("Cable jacket and seals", new Color(.012f, .016f, .02f), 0, .25f);
        lens = IndustrialGeometry.Finish("Optical lens coating", new Color(.025f, .16f, .22f), .6f, .92f);
        white = IndustrialGeometry.Finish("Diffuse inspection LED", new Color(.78f, .85f, .88f), 0, .45f);
        concrete = IndustrialGeometry.Finish("Workshop concrete", new Color(.16f, .18f, .19f), 0, .15f);
        // The floor top is flush with the conveyor foundation underside, so the rig
        // reads as installed on the workshop floor rather than floating above it.
        Box("Workshop floor", new Vector3(0, -1.665f, 0), new Vector3(30, .12f, 36), concrete, .01f);
        var wallFinish = IndustrialGeometry.Finish("Workshop wall finish", new Color(.24f, .27f, .29f), .05f, .2f);
        Box("Inspection shed rear wall", new Vector3(-4, 1.5f, 0), new Vector3(.13f, 6.5f, length + 12), wallFinish);
        for (int bay = -1; bay <= 1; bay++)
        {
            Box("Structural column", new Vector3(-3.87f, 1.25f, bay * (length * .5f + 1)), new Vector3(.18f, 5.8f, .22f), dark);
            Cylinder("Wall service conduit", new Vector3(-3.78f, .3f, bay * (length * .5f + 1) + .2f), .028f, 3.4f, metal, Vector3.up);
        }
        Box("Rear structural beam", new Vector3(-3.86f, 3.4f, 0), new Vector3(.2f, .22f, length + 12), dark);

        // Raised machine plinth and walk-around clearance, scaled to this prototype CAD.
        Box("Concrete foundation", new Vector3(0, -1.53f, 0), new Vector3(4.5f, .15f, length + 3), concrete, .02f);
        Box("Machine mounting skid", new Vector3(0, -1.39f, 0), new Vector3(1.8f, .12f, length + .5f), dark);
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Painted keep-clear boundary", new Vector3(side * 1.85f, -1.448f, 0), new Vector3(.045f, .006f, length + 2), yellow, .002f);
            for (int i = 0; i < 8; i++)
            {
                var stripe = Box("Hazard strip", new Vector3(side * .9f, -1.32f, (i - 3.5f) * length / 8),
                    new Vector3(.18f, .008f, .25f), yellow, .002f);
                stripe.transform.localRotation = Quaternion.Euler(0, 35, 0);
            }
        }

        // Rigid camera gantry with bolted feet, cross-member and a downward-looking optical head.
        for (int side = -1; side <= 1; side += 2)
        {
            Box("Camera gantry mounting foot", new Vector3(side * .72f, -.18f, 0), new Vector3(.36f, .07f, .5f), metal);
            Box("Camera gantry upright", new Vector3(side * .72f, .58f, 0), new Vector3(.1f, 1.5f, .13f), metal);
            Box("Extrusion channel", new Vector3(side * .72f, .58f, -.069f), new Vector3(.018f, 1.43f, .004f), dark, .001f);
            for (int dz = -1; dz <= 1; dz += 2)
                Screw(new Vector3(side * .72f, -.135f, dz * .17f));
        }
        Box("Gantry upper beam", new Vector3(0, 1.32f, 0), new Vector3(1.65f, .13f, .14f), metal);
        Box("Camera adjustable mounting plate", new Vector3(0, 1.19f, -.06f), new Vector3(.3f, .08f, .3f), dark);
        var head = Box("Industrial vision camera", new Vector3(0, .99f, -.06f), new Vector3(.25f, .32f, .25f), dark);
        cameraHead = head.transform;
        Cylinder("C-mount lens barrel", new Vector3(0, .83f, -.06f), .095f, .16f, black, Vector3.down);
        Cylinder("Coated front optical element", new Vector3(0, .665f, -.06f), .075f, .008f, lens, Vector3.down);
        Box("Camera connector", new Vector3(.11f, 1.1f, -.06f), new Vector3(.09f, .065f, .065f), metal);
        Cable("Vision ethernet cable", new[] { new Vector3(.16f, 1.1f, -.06f), new Vector3(.38f, 1.4f, 0),
            new Vector3(.74f, 1.36f, 0), new Vector3(.77f, -.4f, 0) }, .014f);
        foreach (int side in new[] { -1, 1 })
        {
            Box("Inspection light enclosure", new Vector3(side * .42f, .55f, -.06f), new Vector3(.09f, .08f, .7f), dark);
            Box("Inspection LED diffuser", new Vector3(side * .42f, .505f, -.06f), new Vector3(.065f, .01f, .64f), white, .003f);
        }
        Plate("VISION / SPLICE SCAN", new Vector3(0, 1.34f, -.082f), .55f, .065f);

        // Sensors mounted near their actual measurement locations: bearing, drive, shaft and panel.
        float driveZ = length * .43f;
        Cylinder("Bearing accelerometer base", new Vector3(-.58f, .02f, driveZ), .05f, .035f, metal, Vector3.up);
        Cylinder("IEPE vibration sensor", new Vector3(-.58f, .055f, driveZ), .04f, .13f, metal, Vector3.up);
        Cylinder("Vibration connector", new Vector3(-.58f, .185f, driveZ), .025f, .07f, black, Vector3.up);
        Cable("Vibration sensor lead", new[] { new Vector3(-.58f, .24f, driveZ), new Vector3(-.82f, .32f, driveZ - .2f),
            new Vector3(-.85f, -.42f, driveZ - .4f), new Vector3(-.85f, -.45f, -1) }, .009f);
        Cylinder("Bearing temperature probe", new Vector3(.55f, -.12f, driveZ), .022f, .15f, metal, Vector3.right);
        Cylinder("Rotary encoder housing", new Vector3(.66f, -.1f, -driveZ), .11f, .12f, dark, Vector3.right);
        Cylinder("Encoder end cap", new Vector3(.78f, -.1f, -driveZ), .09f, .012f, metal, Vector3.right);
        Cable("Encoder shielded lead", new[] { new Vector3(.81f, -.1f, -driveZ), new Vector3(.97f, -.25f, -driveZ),
            new Vector3(.95f, -.48f, -driveZ) }, .012f);

        // Cable tray and fixed guards leave the inspection run visible.
        Box("Cable tray", new Vector3(.95f, -.55f, 0), new Vector3(.17f, .08f, length * .92f), metal);
        for (int i = 0; i < 12; i++) Box("Cable tray slot", new Vector3(.95f, -.502f, (i - 5.5f) * length / 13), new Vector3(.1f, .004f, .12f), dark, .001f);
        Plate("SIH26008  /  BELT JOINT CONDITION MONITORING", new Vector3(0, -.72f, -length * .5f - .35f), 1.75f, .07f);
    }
    void Awake()
    {
        // When this hierarchy is saved by the editor builder, Build is not called again
        // in Play mode.  Recreate only this transient renderer helper.
        lampColor = new MaterialPropertyBlock();
    }
    void Update()
    {
        // Arduino/Python telemetry is visualised directly in Unity; no cabinet object
        // is required in this student prototype.
    }

    GameObject Box(string name, Vector3 p, Vector3 size, Material m, float bevel = .012f)
        => IndustrialGeometry.Box(name, transform, p, size, m, bevel);
    GameObject Cylinder(string name, Vector3 p, float radius, float length, Material m, Vector3 direction)
    {
        var g = IndustrialGeometry.Cylinder(name, transform, p, radius, length, m);
        g.transform.localRotation = Quaternion.LookRotation(direction, Mathf.Abs(direction.y) > .9f ? Vector3.forward : Vector3.up);
        return g;
    }
    void Cable(string name, Vector3[] points, float radius) => IndustrialGeometry.Cable(name, transform, points, radius, black);
    void Screw(Vector3 p) => Cylinder("Gantry mounting bolt", p, .025f, .016f, dark, Vector3.up);
    void Plate(string text, Vector3 p, float width, float height)
    {
        Box("Equipment nameplate", p, new Vector3(width + .025f, height * 1.65f, .008f), black, .002f);
        var label = new GameObject("Plate " + text).AddComponent<TextMeshPro>();
        label.transform.SetParent(transform, false); label.transform.localPosition = p + Vector3.back * .006f;
        label.transform.localRotation = Quaternion.identity;
        label.fontSize = .8f; label.rectTransform.sizeDelta = new Vector2(width, height * 1.5f);
        label.enableAutoSizing = true; label.fontSizeMin = .05f; label.fontSizeMax = .8f;
        label.text = text; label.alignment = TextAlignmentOptions.Center; label.color = Color.white;
    }
}
