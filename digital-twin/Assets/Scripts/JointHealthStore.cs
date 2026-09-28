using System;
using System.IO;
using UnityEngine;

public class JointHealthStore : MonoBehaviour
{
    [Serializable] private class Snapshot
    {
        public int schema = 1;
        public BeltHealthData[] joints;
        public bool emergencyStop, ruptureEvent;
        public string ruptureJointId;
        public float encoderPosition;
    }
    public string storageDirectory;
    public string storageStatus = "Not initialized";
    private string snapshotFile, historyFile;

    public void Configure(JointHealthManager manager, string storageRoot = null)
    {
        storageDirectory = Path.Combine(storageRoot ?? Application.persistentDataPath, "BeltHealth", manager.demoMode ? "demo" : "live");
        snapshotFile = Path.Combine(storageDirectory, "joint-passports.json");
        historyFile = Path.Combine(storageDirectory, "readings-" + DateTime.UtcNow.ToString("yyyy-MM-dd") + ".jsonl");
        try
        {
            Directory.CreateDirectory(storageDirectory);
            // Demo always starts clean. Real telemetry sessions restore joint records but not motion.
            if (!manager.demoMode && File.Exists(snapshotFile))
            {
                var snapshot = JsonUtility.FromJson<Snapshot>(File.ReadAllText(snapshotFile));
                if (snapshot == null || snapshot.schema != 1 || snapshot.joints == null || snapshot.joints.Length != 5)
                    throw new InvalidDataException("Invalid passport file");
                for (int i = 0; i < 5; i++)
                    if (snapshot.joints[i] == null || !snapshot.joints[i].IsValid() || snapshot.joints[i].joint_id != $"J-{i + 1:00}")
                        throw new InvalidDataException("Invalid joint record");
                manager.joints = snapshot.joints;
                if (snapshot.ruptureEvent && (!snapshot.emergencyStop || !JointHealthManager.IsJointId(snapshot.ruptureJointId)))
                    throw new InvalidDataException("Invalid saved rupture latch");
                manager.rupture.geometry.loop.travelMeters = snapshot.encoderPosition;
                manager.rupture.emergencyStop = snapshot.emergencyStop;
                manager.rupture.ruptureEvent = snapshot.ruptureEvent;
                manager.rupture.ruptureJointId = snapshot.ruptureJointId;
                if (snapshot.ruptureEvent) manager.rupture.geometry.Build(snapshot.ruptureJointId);
                // Build geometry from stored critical joint if needed, but latest remains null (no motion).
                manager.Refresh();
                if (snapshot.emergencyStop) manager.rupture.TriggerOperatorEmergency();
                manager.dataStatus = "Restored passports; awaiting live telemetry";
            }
            storageStatus = "Recording to " + storageDirectory;
        }
        catch (Exception error)
        {
            storageStatus = error.Message;
            if (!manager.demoMode) { manager.rupture.TriggerOperatorEmergency(); manager.dataStatus = "Passport recovery failed; motion inhibited"; }
            Debug.LogWarning("[Passports] " + error.Message);
        }
    }

    public void Record(BeltHealthData data, JointHealthManager manager)
    {
        if (string.IsNullOrEmpty(historyFile)) return;
        try { File.AppendAllText(historyFile, JsonUtility.ToJson(data) + Environment.NewLine); Save(manager); }
        catch (Exception error) { storageStatus = error.Message; }
    }

    public void Save(JointHealthManager manager)
    {
        if (string.IsNullOrEmpty(snapshotFile)) return;
        try
        {
            var snapshot = new Snapshot { joints = manager.joints, emergencyStop = manager.rupture.emergencyStop,
                ruptureEvent = manager.rupture.ruptureEvent, ruptureJointId = manager.rupture.ruptureJointId,
                encoderPosition = manager.rupture.geometry ? manager.rupture.geometry.loop.travelMeters : 0 };
            string temporary = snapshotFile + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(snapshot, true));
            if (File.Exists(snapshotFile)) File.Replace(temporary, snapshotFile, null);
            else File.Move(temporary, snapshotFile);
        }
        catch (Exception error) { storageStatus = error.Message; }
    }
}
