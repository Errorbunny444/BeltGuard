using UnityEngine;

public class BoltIdentity : MonoBehaviour
{
    public enum BoltType { Unknown, Good, Rust, Bent, Broken, White, Black }

    public BoltType boltType = BoltType.Unknown;

    [HideInInspector]
    public BoltType hiddenType = BoltType.Good;
    public bool isInspected = false;

    void Start()
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null) rb = gameObject.AddComponent<Rigidbody>();

        rb.mass = 1f;
        rb.linearDamping = 1.5f;
        rb.angularDamping = 0.5f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        ApplyIdentity();
    }

    public void ApplyIdentity()
    {
        Renderer rend = GetComponent<Renderer>() ?? GetComponentInChildren<Renderer>();
        if (rend == null) return;

        Material mat = new Material(rend.material);
        Quaternion flatRotation = Quaternion.Euler(0f, 0f, 90f);
        transform.localRotation = flatRotation;
        transform.localScale    = new Vector3(0.3f, 0.6f, 0.3f);

        switch (boltType)
        {
            case BoltType.Unknown:  mat.color = new Color(0.6f, 0.6f, 0.6f);   break;
            case BoltType.Good:     mat.color = new Color(0.2f, 0.75f, 0.2f);  break;
            case BoltType.Rust:     mat.color = new Color(0.6f, 0.3f, 0.1f);   break;
            case BoltType.Broken:   mat.color = new Color(0.5f, 0.05f, 0.05f);
                                    transform.localScale = new Vector3(0.3f, 0.4f, 0.3f); break;
            case BoltType.Bent:     mat.color = new Color(0.9f, 0.6f, 0.1f);
                                    transform.localRotation = Quaternion.Euler(0f, 0f, 65f); break;

            // ✅ Chess piece colours
            case BoltType.White:    mat.color = Color.white; break;
            case BoltType.Black:    mat.color = Color.black; break;
        }

        rend.material = mat;
    }

    public void RevealType()
    {
        boltType = hiddenType;
        ApplyIdentity();
        Debug.Log("[Reveal] " + boltType);
    }

    public void SetType(BoltType type)
    {
        boltType = type;
        ApplyIdentity();
    }
}