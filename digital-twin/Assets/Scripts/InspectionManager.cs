using System.Collections.Generic;
using UnityEngine;

public class InspectionManager : MonoBehaviour
{
    public static InspectionManager Instance { get; private set; }

    public int totalBolts;
    public int goodCount;
    public int rustCount;
    public int bentCount;
    public int brokenCount;

    public event System.Action<GameObject, BoltIdentity.BoltType> OnBoltInspected;

    private readonly List<GameObject> activeBolts = new List<GameObject>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void RegisterBolt(GameObject bolt)
    {
        activeBolts.Add(bolt);
        totalBolts++;
    }

    public void InspectBolt(GameObject bolt)
    {
        BoltIdentity id = bolt.GetComponent<BoltIdentity>();
        if (id == null)
        {
            return;
        }

        switch (id.boltType)
        {
            case BoltIdentity.BoltType.Unknown:
                return;

            case BoltIdentity.BoltType.Good:
                goodCount++;
                break;

            case BoltIdentity.BoltType.Rust:
                rustCount++;
                break;

            case BoltIdentity.BoltType.Bent:
                bentCount++;
                break;

            case BoltIdentity.BoltType.Broken:
                brokenCount++;
                break;
        }

        Debug.Log("[Inspection] " + id.boltType + " | Good:" + goodCount + " Rust:" + rustCount + " Bent:" + bentCount + " Broken:" + brokenCount);
        OnBoltInspected?.Invoke(bolt, id.boltType);
    }

    public float DefectPercentage()
    {
        int inspected = goodCount + rustCount + bentCount + brokenCount;
        if (inspected == 0)
        {
            return 0f;
        }

        return ((rustCount + bentCount + brokenCount) / (float)inspected) * 100f;
    }

    public float GoodPercentage()
    {
        int inspected = goodCount + rustCount + bentCount + brokenCount;
        if (inspected == 0)
        {
            return 0f;
        }

        return (goodCount / (float)inspected) * 100f;
    }

    public void ResetCounters()
    {
        totalBolts = goodCount = rustCount = bentCount = brokenCount = 0;
        activeBolts.Clear();
    }
}
