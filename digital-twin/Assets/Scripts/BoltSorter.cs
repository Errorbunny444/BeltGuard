using System.Collections;
using UnityEngine;

public class BoltSorter : MonoBehaviour
{
    public Transform gateTransform;
    public float rotationSpeed = 8f;
    public float neutralAngle = 0f;
    public float goodAngle = -45f;
    public float badAngle = 45f;
    public float gateHoldTime = 1.5f;

    private float targetAngle;
    private bool isMoving = false;

    void Start()
    {
        targetAngle = neutralAngle;
    }

    void Update()
    {
        if (gateTransform == null)
        {
            return;
        }

        float currentY = gateTransform.localEulerAngles.y;
        float newY = Mathf.LerpAngle(currentY, targetAngle, Time.deltaTime * rotationSpeed);
        gateTransform.localEulerAngles = new Vector3(0f, newY, 0f);
    }

    void OnTriggerEnter(Collider other)
    {
        if (isMoving)
        {
            return;
        }

        BoltIdentity bolt = other.GetComponent<BoltIdentity>();
        if (bolt == null)
        {
            return;
        }

        if (bolt.boltType == BoltIdentity.BoltType.Unknown)
        {
            return;
        }

        if (bolt.boltType == BoltIdentity.BoltType.Good)
        {
            StartCoroutine(MoveGate(goodAngle));
        }
        else
        {
            StartCoroutine(MoveGate(badAngle));
        }
    }

    IEnumerator MoveGate(float angle)
    {
        isMoving = true;
        targetAngle = angle;
        yield return new WaitForSeconds(gateHoldTime);
        targetAngle = neutralAngle;
        yield return new WaitForSeconds(0.5f);
        isMoving = false;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
        Gizmos.DrawWireCube(transform.position, new Vector3(1f, 2f, 3f));
    }
}
