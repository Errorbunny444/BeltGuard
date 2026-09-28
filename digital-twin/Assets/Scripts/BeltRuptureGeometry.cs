using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Clips the actual CAD belt into two static pieces. No soft-body or unstable tearing physics.
public class BeltRuptureGeometry : MonoBehaviour
{
    public MeshFilter source;
    public BeltLoopMotion loop;
    public JointHealthManager manager;
    public BeltRuptureStateManager states;
    private Mesh leftMesh, rightMesh;
    private struct Vertex
    {
        public Vector3 position, normal;
        public Vector2 uv;
        public static Vertex Lerp(Vertex a, Vertex b, float t) => new Vertex {
            position = Vector3.Lerp(a.position, b.position, t),
            normal = Vector3.Lerp(a.normal, b.normal, t).normalized, uv = Vector2.Lerp(a.uv, b.uv, t) };
    }

    public void Build(string jointId = null)
    {
        int index = (jointId ?? manager.WorstJoint().joint_id)[3] - '1';
        loop.JointPose(index, out var cut, out _, out _);
        // The static rupture view cuts both strands at the affected longitudinal position.
        var axis = loop.axis;
        var mesh = source.sharedMesh;
        var positions = mesh.vertices; var normals = mesh.normals; var uv = mesh.uv;
        var vertices = new Vertex[positions.Length];
        var normalMatrix = source.transform.localToWorldMatrix.inverse.transpose;
        for (int i = 0; i < vertices.Length; i++) vertices[i] = new Vertex {
            position = source.transform.TransformPoint(positions[i]),
            normal = normals.Length > i ? normalMatrix.MultiplyVector(normals[i]).normalized : Vector3.up,
            uv = uv.Length > i ? uv[i] : Vector2.zero };
        if (leftMesh) Destroy(leftMesh); if (rightMesh) Destroy(rightMesh);
        leftMesh = Clip(vertices, mesh.triangles, cut, axis, false);
        rightMesh = Clip(vertices, mesh.triangles, cut, axis, true);
        states.rupturedLeft.GetComponent<MeshFilter>().sharedMesh = leftMesh;
        states.rupturedRight.GetComponent<MeshFilter>().sharedMesh = rightMesh;
    }

    private Mesh Clip(Vertex[] vertices, int[] triangles, Vector3 cut, Vector3 axis, bool positive)
    {
        var points = new List<Vector3>(); var normals = new List<Vector3>();
        var uv = new List<Vector2>(); var indices = new List<int>();
        // A clipped triangle has at most four vertices. Reuse scratch storage per piece.
        var polygon = new List<Vertex>(4);
        for (int t = 0; t < triangles.Length; t += 3)
        {
            polygon.Clear();
            for (int i = 0; i < 3; i++)
            {
                Vertex a = vertices[triangles[t + i]], b = vertices[triangles[t + (i + 1) % 3]];
                float da = FractureDistance(a.position, cut, axis), db = FractureDistance(b.position, cut, axis);
                bool insideA = positive ? da >= 0 : da <= 0;
                bool insideB = positive ? db >= 0 : db <= 0;
                if (insideA) polygon.Add(a);
                if (insideA != insideB) polygon.Add(Vertex.Lerp(a, b, da / (da - db)));
            }
            int offset = points.Count;
            foreach (var vertex in polygon)
            {
                points.Add(transform.InverseTransformPoint(vertex.position));
                normals.Add(vertex.normal); uv.Add(vertex.uv);
            }
            for (int i = 1; i < polygon.Count - 1; i++)
            { indices.Add(offset); indices.Add(offset + i); indices.Add(offset + i + 1); }
        }
        var result = new Mesh { name = positive ? "CAD belt - right rupture piece" : "CAD belt - left rupture piece", indexFormat = IndexFormat.UInt32 };
        result.SetVertices(points); result.SetNormals(normals); result.SetUVs(0, uv); result.SetTriangles(indices, 0);
        result.RecalculateBounds(); return result;
    }
    float FractureDistance(Vector3 point, Vector3 cut, Vector3 axis)
    {
        // Same boundary for both pieces; only the cut's appearance changes.
        // Small irregularity avoids a machine-sawn edge without soft-body simulation.
        float width = loop.axis.x != 0 ? loop.bounds.size.z : loop.bounds.size.x;
        float across = Vector3.Dot(point-cut,loop.across) / Mathf.Max(width,.01f);
        float edge = width*.018f*(Mathf.Sin(across*23)+.35f*Mathf.Sin(across*61));
        return Vector3.Dot(point-cut,axis)-edge;
    }
    void OnDestroy() { if (leftMesh) Destroy(leftMesh); if (rightMesh) Destroy(rightMesh); }
}
