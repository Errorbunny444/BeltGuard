using UnityEngine;

[DefaultExecutionOrder(110)]
public class BeltAnimation : MonoBehaviour
{
    private Renderer rend;
    private int texturePropertyId = -1;
    private Material surface;
    private Material originalSurface;

    public StartButton script;
    public ConveyorSpeedControl spc;

    private float offset = 0f;
    private Vector2 calibratedOffset;
    public bool useCalibratedUV;
    public Vector2 uvPerWorldUnit;
    private Mesh motionMesh;

    // One UV unit equals one full circuit. Only a runtime copy of the CAD is changed.
    public void MapContinuousLoop(Vector3 axis)
    {
        var filter = GetComponent<MeshFilter>();
        if (!filter || !filter.sharedMesh || !filter.sharedMesh.isReadable)
            throw new System.InvalidOperationException("Belt mesh must have Read/Write enabled.");
        Bounds bounds = GetComponent<Renderer>().bounds;
        float radius = bounds.extents.y;
        float straight = (axis.x != 0 ? bounds.size.x : bounds.size.z) - 2 * radius;
        float perimeter = 2 * straight + 2 * Mathf.PI * radius;
        Vector3 across = axis.x != 0 ? Vector3.forward : Vector3.right;
        float width = axis.x != 0 ? bounds.size.z : bounds.size.x;
        motionMesh = Instantiate(filter.sharedMesh);
        motionMesh.name = "Runtime belt continuous travel UV";
        var vertices = motionMesh.vertices;
        var uv = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 p = transform.TransformPoint(vertices[i]) - bounds.center;
            float x = Vector3.Dot(p, axis), y = p.y, distance;
            if (x > straight * .5f)
                distance = straight + radius * (Mathf.PI * .5f - Mathf.Atan2(y, x - straight * .5f));
            else if (x < -straight * .5f)
            {
                float angle = Mathf.Atan2(y, x + straight * .5f);
                if (angle > 0) angle -= 2 * Mathf.PI;
                distance = 2 * straight + Mathf.PI * radius + radius * (-Mathf.PI * .5f - angle);
            }
            else distance = y >= 0 ? x + straight * .5f
                : straight + Mathf.PI * radius + straight * .5f - x;
            uv[i] = new Vector2(distance / perimeter, Vector3.Dot(p, across) / width + .5f);
        }
        motionMesh.uv = uv;
        motionMesh.RecalculateTangents();
        filter.sharedMesh = motionMesh;
        useCalibratedUV = true;
        uvPerWorldUnit = new Vector2(1 / perimeter, 0);
    }

    void OnDestroy()
    {
        if (motionMesh) Destroy(motionMesh);
        // This component owns only its explicit runtime copy, never an imported asset.
        if (rend && surface && rend.sharedMaterial == surface) rend.sharedMaterial = originalSurface;
        if (surface) Destroy(surface);
    }

    // Measure the imported CAD UV layout instead of assuming one UV repeat per loop.
    public void CalibrateSurface(Vector3 travelAxis)
    {
        var filter = GetComponent<MeshFilter>();
        if (!filter || !filter.sharedMesh || !filter.sharedMesh.isReadable) return;
        var mesh = filter.sharedMesh;
        var vertices = mesh.vertices; var uv = mesh.uv; var triangles = mesh.triangles;
        if (uv.Length != vertices.Length) return;
        float top = GetComponent<Renderer>().bounds.max.y;
        Vector2 sum = Vector2.zero; float weight = 0;
        for (int i = 0; i < triangles.Length; i += 3)
        {
            int a = triangles[i], b = triangles[i + 1], c = triangles[i + 2];
            Vector3 p = transform.TransformPoint(vertices[a]);
            Vector3 q = transform.TransformPoint(vertices[b]);
            Vector3 r = transform.TransformPoint(vertices[c]);
            // Use only the flat upper carrying surface, not the pulley wraps or edges.
            if (Mathf.Abs(p.y - top) > .02f || Mathf.Abs(q.y - top) > .02f || Mathf.Abs(r.y - top) > .02f) continue;
            Vector3 e = q - p, f = r - p;
            float ee = Vector3.Dot(e, e), ff = Vector3.Dot(f, f), ef = Vector3.Dot(e, f);
            float determinant = ee * ff - ef * ef;
            if (determinant <= 1e-8f) continue;
            float de = Vector3.Dot(travelAxis, e), df = Vector3.Dot(travelAxis, f);
            Vector2 gradient = (uv[b] - uv[a]) * ((de * ff - df * ef) / determinant)
                + (uv[c] - uv[a]) * ((df * ee - de * ef) / determinant);
            float area = Vector3.Cross(e, f).magnitude;
            sum += gradient * area; weight += area;
        }
        if (weight > 0)
        {
            uvPerWorldUnit = sum / weight;
            useCalibratedUV = uvPerWorldUnit.sqrMagnitude > 1e-10f;
        }
    }

    public float visualMultiplier = 0.25f;
    public float directionMultiplier = -1f;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend == null)
        {
            rend = GetComponentInChildren<Renderer>();
        }

        if (rend != null)
        {
            originalSurface = rend.sharedMaterial;
            if (!originalSurface) return;
            surface = new Material(originalSurface) { name = originalSurface.name + " (Belt Animation)" };
            rend.sharedMaterial = surface;
            if (surface.HasProperty("_BaseMap"))
            {
                texturePropertyId = Shader.PropertyToID("_BaseMap");
            }
            else if (surface.HasProperty("_MainTex"))
            {
                texturePropertyId = Shader.PropertyToID("_MainTex");
            }
        }
    }

    void Update()
    {
        if (script == null || !script.start) return;
        if (spc == null || rend == null || texturePropertyId == -1) return;

        if (useCalibratedUV)
        {
            // Same linear speed as the pulley (angular speed = linear speed / radius).
            calibratedOffset = -Vector2.Scale(uvPerWorldUnit, surface.GetTextureScale(texturePropertyId))
                * (float)spc.TravelDistance;
            calibratedOffset.x = Mathf.Repeat(calibratedOffset.x, 1);
            calibratedOffset.y = Mathf.Repeat(calibratedOffset.y, 1);
            surface.SetTextureOffset(texturePropertyId, calibratedOffset);
            if (surface.HasProperty("_BumpMap")) surface.SetTextureOffset("_BumpMap", calibratedOffset);
            return;
        }

        // UV offset is measured in texture repeats, so include the tiling factor.
        offset = Mathf.Repeat(offset + Time.deltaTime * spc.ConvSpeed * visualMultiplier
            * directionMultiplier * surface.GetTextureScale(texturePropertyId).x, 1f);

        surface.SetTextureOffset(texturePropertyId, new Vector2(offset, 0));
        if (surface.HasProperty("_BumpMap"))
            surface.SetTextureOffset("_BumpMap", new Vector2(offset, 0));
    }
}
