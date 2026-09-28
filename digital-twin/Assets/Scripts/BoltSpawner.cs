using UnityEngine;

public class BoltSpawner : MonoBehaviour
{
    public GameObject boltPrefab;
    public bool spawningEnabled = true;
    public float spawnInterval = 6f;
    public float spawnHeightOffset = 0.05f;
    [Range(0, 100)] public int goodPercent = 70;
    [Range(0, 100)] public int rustPercent = 10;
    [Range(0, 100)] public int bentPercent = 10;
    public StartButton startScript;

    private float timer = 0f;
    private ConveyorSpeedControl speedControl;

    void Update()
    {
        if (!spawningEnabled)
        {
            return;
        }

        if (startScript == null || !startScript.start)
        {
            return;
        }

        if (speedControl == null)
        {
            speedControl = Object.FindAnyObjectByType<ConveyorSpeedControl>();
        }

        if (speedControl != null && Mathf.Approximately(speedControl.BeltSpeed, 0f))
        {
            return;
        }

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnBolt();
        }
    }

    void SpawnBolt()
    {
        if (boltPrefab == null)
        {
            Debug.LogWarning("[BoltSpawner] No prefab");
            return;
        }

        Vector3 spawnPosition = transform.position + Vector3.up * spawnHeightOffset;
        GameObject bolt = Instantiate(boltPrefab, spawnPosition, Quaternion.identity);
        BoltIdentity id = bolt.GetComponent<BoltIdentity>();
        if (id == null)
        {
            Debug.LogWarning("[BoltSpawner] No BoltIdentity");
            return;
        }

        id.hiddenType = GetRandomType();
        id.SetType(BoltIdentity.BoltType.Unknown);
        InspectionManager.Instance?.RegisterBolt(bolt);
    }

    BoltIdentity.BoltType GetRandomType()
    {
        int roll = Random.Range(0, 100);

        if (roll < goodPercent)
        {
            return BoltIdentity.BoltType.Good;
        }

        if (roll < goodPercent + rustPercent)
        {
            return BoltIdentity.BoltType.Rust;
        }

        if (roll < goodPercent + rustPercent + bentPercent)
        {
            return BoltIdentity.BoltType.Bent;
        }

        return BoltIdentity.BoltType.Broken;
    }

    public void ForceSpawn(BoltIdentity.BoltType type)
    {
        if (boltPrefab == null)
        {
            Debug.LogWarning("[BoltSpawner] No prefab");
            return;
        }

        Vector3 spawnPosition = transform.position + Vector3.up * spawnHeightOffset;
        GameObject bolt = Instantiate(boltPrefab, spawnPosition, Quaternion.identity);
        BoltIdentity id = bolt.GetComponent<BoltIdentity>();
        if (id == null)
        {
            Debug.LogWarning("[BoltSpawner] No BoltIdentity");
            return;
        }

        id.hiddenType = type;
        id.SetType(type);
        InspectionManager.Instance?.RegisterBolt(bolt);
    }
}
