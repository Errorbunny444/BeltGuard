using TMPro;
using UnityEngine;

public class DamageVisualizer : MonoBehaviour
{
    public Transform[] jointMarkers = new Transform[5];
    public TMP_Text[] jointLabels = new TMP_Text[5];
    public GameObject damageMarker, warningLight, emergencyAlarmText;
    public TMP_Text alarmText;
    public float markerHeight = 0.1f;
    public bool colorSpliceMarkers;
    private MaterialPropertyBlock colors;
    void Awake() { colors = new MaterialPropertyBlock(); }

    public void Refresh(JointHealthManager manager)
    {
        var worst = manager.WorstJoint();
        bool stopped = manager.rupture && manager.rupture.emergencyStop;
        for (int i = 0; i < jointMarkers.Length && i < manager.joints.Length; i++)
        {
            if (!jointMarkers[i]) continue;
            var data = manager.joints[i];
            bool observed = !string.IsNullOrEmpty(data.last_updated) && !data.last_updated.StartsWith("Never");
            Color color = !observed ? Color.gray : data.health_score >= 80 ? new Color(.26f, .32f, .28f) : data.health_score >= 60 ? Color.yellow :
                data.health_score >= 40 ? new Color(1, 0.45f, 0) : Color.red;
            var renderer = jointMarkers[i].GetComponent<Renderer>();
            if (renderer)
            {
                if (!colorSpliceMarkers) color = new Color(.11f, .12f, .12f);
                colors.SetColor("_BaseColor", color);
                colors.SetColor("_Color", color);
                renderer.SetPropertyBlock(colors);
            }
            var label = i < jointLabels.Length ? jointLabels[i] : null;
            if (label) label.text = observed ? $"{data.joint_id}  /  {data.health_score:0}\n{data.risk_level.ToUpperInvariant()}" : $"{data.joint_id}\nAWAITING DATA";
        }
        if (damageMarker)
        {
            damageMarker.SetActive(stopped || worst.health_score < 60 || worst.damage_detected);
            int index = worst.joint_id[3] - '1';
            if (index < jointMarkers.Length && jointMarkers[index])
                damageMarker.transform.position = jointMarkers[index].position + Vector3.up * markerHeight;
        }
        if (warningLight) warningLight.SetActive(stopped || worst.health_score < 40);
        if (emergencyAlarmText) emergencyAlarmText.SetActive(stopped || worst.health_score < 40);
        if (alarmText) alarmText.text = stopped
            ? manager.rupture.ruptureEvent ? "RUPTURE / STOP LATCHED\nIsolate conveyor. Splice maintenance required." : "OPERATOR EMERGENCY STOP\nInvestigate cause before reset."
            : $"CRITICAL JOINT: {worst.joint_id}\n{worst.maintenance_action}";
    }
}
