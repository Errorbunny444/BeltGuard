using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Cosmetic splice damage only. Fixed per-joint patterns grow without random flicker.
[DefaultExecutionOrder(200)]
public class JointSurfaceDamage : MonoBehaviour
{
    [Header("Runtime references (assigned by scene setup)")]
    public JointHealthManager manager;
    public BeltLoopMotion loop;
    [Tooltip("Belt width in Unity world units. Do not substitute the hardware width in metres.")]
    public float beltWidth;
    readonly MeshRenderer[] cracks = new MeshRenderer[5];
    readonly Mesh[] meshes = new Mesh[5];
    readonly float[] visibleDamage = new float[5];
    // Reused across joints; Mesh.SetVertices copies the data into each owned mesh.
    // Maximum detail: 226 vertices / 198 triangles, across four material layers.
    readonly List<Vector3> vertices = new List<Vector3>(512);
    readonly List<int>[] triangles = { new List<int>(600), new List<int>(600), new List<int>(900), new List<int>(90) };
    Material[] materials;
    MaterialPropertyBlock appearance;
    Vector3 origin, normal, tangent;
    Transform surface;
    float severity, extent;
    int jointIndex, layer; // Layers: scuff, rubber lip, split/branches, reinforcement.
    bool wearOnly;
    float defectOffset;

    void Awake() { appearance = new MaterialPropertyBlock(); }
    void Start()
    {
        // Reuse the URP shader already used by the existing damage visualization.
        var shader = Shader.Find("Universal Render Pipeline/Unlit");
        materials = new Material[4];
        string[] names = { "Worn rubber", "Frayed crack edge", "Recessed split", "Exposed reinforcement" };
        for (int n = 0; n < 4; n++)
        {
            materials[n] = new Material(shader) { name = names[n] };
            materials[n].SetFloat("_Cull", 0);
        }
        for (int i = 0; i < 5; i++)
        {
            var item = new GameObject($"Physical crack J-{i + 1:00}", typeof(MeshFilter), typeof(MeshRenderer));
            item.transform.SetParent(transform, false);
            cracks[i] = item.GetComponent<MeshRenderer>(); cracks[i].sharedMaterials = materials;
            cracks[i].shadowCastingMode = ShadowCastingMode.Off;
            meshes[i] = new Mesh { name = $"Worn splice and jagged tear J-{i + 1:00}" };
            meshes[i].MarkDynamic(); item.GetComponent<MeshFilter>().sharedMesh = meshes[i];
        }
    }

    void LateUpdate()
    {
        if (!manager || !loop || !manager.rupture) return;
        for (int i = 0; i < 5; i++)
        {
            var data = manager.joints[i]; var line = cracks[i];
            float desired = data.damage_detected ? data.damage_severity : 0;
            visibleDamage[i] = Mathf.Lerp(visibleDamage[i], desired, 1 - Mathf.Exp(-8 * Time.unscaledDeltaTime));
            // During the short failure reveal the critical crack remains visible until
            // the static rupture pieces take over.
            line.enabled = (!manager.rupture.ruptureEvent || manager.rupture.IsRuptureTransitioning)
                && visibleDamage[i] > .01f;
            if (!line.enabled) continue;
            jointIndex = i; severity = visibleDamage[i] / 100;
            extent = beltWidth * .46f * Mathf.Sqrt(severity); surface = line.transform;
            wearOnly = DamageReferenceProfile.IsWear(data.damage_type);
            bool hole = DamageReferenceProfile.IsHole(data.damage_type);
            bool small = DamageReferenceProfile.IsSmall(data.damage_type);
            string kind = DamageReferenceProfile.Normalize(data.damage_type);
            if (small) extent *= .35f;
            else if (kind == "large_tear") extent *= .8f;
            defectOffset = (small || hole) ? beltWidth * ((i % 3 - 1) * .16f) : 0;
            loop.JointPose(i, out origin, out normal, out tangent);
            vertices.Clear(); foreach (var indices in triangles) indices.Clear();
            // Wear is cover abrasion, not a hole. It fades into the first open split
            // near the existing wear-to-crack scenario boundary.
            if (wearOnly)
            {
                layer = 0;
                for (int mark = 0; mark < 9; mark++) Scuff(mark);
            }
            else if (hole)
            {
                float radius = beltWidth * (small ? .018f : .052f) * Mathf.Sqrt(severity);
                layer = 1; Puncture(radius * 1.18f, .007f);
                layer = 2; Puncture(radius, .008f);
            }
            else
            {
            // Layered lip and dark centre suggest depth without cutting the animated CAD.
            layer = 0; Ribbon(2.5f, .006f);
            layer = 1; Ribbon(1.35f, .007f);
            layer = 2; Ribbon(1, .008f);
            float growth = Mathf.InverseLerp(.3f, .8f, severity);
            for (int branch = 0; branch < 4; branch++) Branch(branch, growth);
            layer = 3;
            float exposure = Mathf.InverseLerp(.65f, .95f, severity);
            if (exposure > 0) for (int strand = 0; strand < 7; strand++) Strand(strand, exposure);
            }
            meshes[i].Clear(); meshes[i].SetVertices(vertices); meshes[i].subMeshCount = 4;
            for (int n = 0; n < 4; n++) meshes[i].SetTriangles(triangles[n], n, false);
            meshes[i].RecalculateBounds();
            Tint(line, 0, Color.Lerp(new Color(.09f,.26f,.07f), new Color(.16f,.23f,.105f), Mathf.Clamp01(severity*3)));
            Tint(line, 1, Color.Lerp(new Color(.09f,.26f,.07f), new Color(.36f,.35f,.24f), Mathf.Clamp01(severity*1.5f)));
            float opening = (kind == "splice_crack") ? Mathf.SmoothStep(0,1,Mathf.InverseLerp(.40f,.58f,severity)) : Mathf.Clamp01(severity*2.6f);
            Tint(line, 2, Color.Lerp(new Color(.09f,.26f,.07f), new Color(.009f,.012f,.007f), opening));
            Tint(line, 3, Color.Lerp(new Color(.035f,.04f,.025f), new Color(.43f,.40f,.27f), Mathf.InverseLerp(.65f,.95f,severity)));
        }
    }
    void Scuff(int mark)
    {
        float x = (mark / 8f - .5f) * extent * 1.6f;
        float y = (Noise(mark) - .5f) * beltWidth * .07f;
        float halfLength = beltWidth * (.025f + Noise(mark+4)*.04f) * severity;
        float halfWidth = beltWidth * .0015f * severity;
        int start = vertices.Count;
        Add(x-halfWidth,y-halfLength,beltWidth*.006f); Add(x+halfWidth,y-halfLength,beltWidth*.006f);
        Add(x-halfWidth,y+halfLength,beltWidth*.006f); Add(x+halfWidth,y+halfLength,beltWidth*.006f);
        Quad(start);
    }
    void Puncture(float radius, float height)
    {
        // Star-shaped perimeter reflects the annotated punctures, not a perfect disk.
        int start = vertices.Count;
        Add(0,0,beltWidth*height);
        for (int p=0;p<=20;p++)
        {
            float angle = (p%20)/20f*Mathf.PI*2;
            float r = radius*(.7f+.3f*Noise(p%20));
            Add(Mathf.Cos(angle)*r,Mathf.Sin(angle)*r*.8f,beltWidth*height);
            if(p>0) { triangles[layer].Add(start); triangles[layer].Add(start+p); triangles[layer].Add(start+p+1); }
        }
    }
    void Tint(Renderer renderer, int slot, Color color)
    { appearance.SetColor("_BaseColor", color); renderer.SetPropertyBlock(appearance, slot); }
    float Noise(float x) => Mathf.PerlinNoise(x * 2.7f + jointIndex * 11.31f, 3.19f);
    float Path(float t) => beltWidth * severity * (.038f * (Noise(t*5)-.5f) + .009f * Mathf.Sin(t*57+jointIndex));
    float Opening(float t) => beltWidth * .021f * severity * Mathf.Pow(Mathf.Max(0,Mathf.Sin(Mathf.PI*t)),.65f) * (.55f+Noise(t*9));

    void Ribbon(float scale, float height)
    {
        int start = vertices.Count;
        for (int p = 0; p <= 24; p++)
        {
            float t = p/24f, x = (t*2-1)*extent, y = Path(t), w = Opening(t)*scale;
            Add(x,y-w,height*beltWidth);
            Add(x,y+w*(.7f+Noise(t*7)*.6f),height*beltWidth);
            if (p>0) Quad(start+(p-1)*2);
        }
    }
    void Branch(int branch, float growth)
    {
        if (growth<=0) return;
        float at = .2f+branch*.19f, sign = branch%2==0 ? -1 : 1;
        float x = (at*2-1)*extent, y = Path(at);
        int start = vertices.Count;
        for (int p=0; p<=5; p++)
        {
            float t=p/5f, px=x+extent*.12f*t;
            float py=y+sign*beltWidth*.085f*growth*t+beltWidth*.004f*growth*Mathf.Sin(p*2.3f);
            float w=beltWidth*.004f*growth*(1-t);
            Add(px,py-w,beltWidth*.0085f); Add(px,py+w,beltWidth*.0085f);
            if(p>0) Quad(start+(p-1)*2);
        }
    }
    void Strand(int strand, float exposure)
    {
        float t=.24f+strand*.075f, x=(t*2-1)*extent, y=Path(t), span=Opening(t)*.9f;
        float w=beltWidth*.0008f*exposure;
        int start=vertices.Count;
        Add(x-w,y-span,beltWidth*.009f); Add(x+w,y-span,beltWidth*.009f);
        Add(x-w+span*.2f,y+span,beltWidth*.009f); Add(x+w+span*.2f,y+span,beltWidth*.009f);
        Quad(start);
    }
    void Add(float across,float along,float lift)
    { vertices.Add(surface.InverseTransformPoint(origin+loop.across*(across+defectOffset)+tangent*along+normal*lift)); }
    void Quad(int a)
    {
        var indices=triangles[layer];
        indices.Add(a); indices.Add(a+1); indices.Add(a+2);
        indices.Add(a+1); indices.Add(a+3); indices.Add(a+2);
    }
    void OnDestroy()
    {
        if(materials!=null) foreach(var material in materials) if(material) Destroy(material);
        foreach(var mesh in meshes) if(mesh) Destroy(mesh);
    }
}
