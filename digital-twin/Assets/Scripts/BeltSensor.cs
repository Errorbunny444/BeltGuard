using UnityEngine;

public class BeltSensor : MonoBehaviour
{
    public bool sensorActive = true;

    void OnDrawGizmos()
    {
        Gizmos.color = sensorActive ? Color.cyan : Color.gray;
        Gizmos.DrawWireCube(transform.position, transform.localScale);
    }

    void OnTriggerEnter(Collider other)
    {
        if (!sensorActive)
        {
            return;
        }

        BoltIdentity bolt = other.GetComponent<BoltIdentity>();
        if (bolt == null)
        {
            return;
        }

        bolt.RevealType();
        Debug.Log("[Sensor] Revealed: " + bolt.boltType);
        InspectionManager.Instance?.InspectBolt(other.gameObject);
    }
}
