using UnityEngine;

// A deterministic demonstration model, not a trained failure predictor.
// Lower-health scenarios accumulate damage; repair requires the explicit demo reset.
public class JointDegradationModel
{
    private static readonly float[] Targets = { 100, 72, 52, 25, 0 };
    public float targetHealth = 100;

    public void Request(int stage)
    {
        targetHealth = Targets[Mathf.Clamp(stage, 1, 5) - 1];
    }

    public void Step(BeltHealthData data, float seconds, float healthLossPerSecond)
    {
        float target = Mathf.Min(data.health_score, targetHealth);
        data.health_score = Mathf.MoveTowards(data.health_score, target,
            Mathf.Max(0, seconds) * Mathf.Max(0, healthLossPerSecond));
        // Preserve the existing <10 rupture interlock. Failure becomes a zero-health
        // event at that threshold instead of leaving a ruptured joint at 9% health.
        if (data.health_score < 10) data.health_score = 0;
        float damage = 100 - data.health_score;
        data.damage_severity = damage;
        data.damage_detected = damage > .01f;
        data.damage_type = damage <= .01f ? "none" : data.health_score >= 60 ? "surface_wear" : "splice_crack";
        data.vibration = Mathf.Lerp(1, 10, damage / 100);
        data.temperature = Mathf.Lerp(30, 80, damage / 100);
        data.motor_current = Mathf.Lerp(1.2f, 5, damage / 100);
        data.confidence = data.damage_detected ? .91f : 0;
        data.maintenance_action = DamageReferenceProfile.Advice(data.damage_type, data.health_score);
    }
}
