using UnityEngine;

public static class MaintenanceAdvisor
{
    public static string Condition(float health)
        => health >= 99.99f ? "Healthy" : health >= 60 ? "Surface Wear" :
            health >= 40 ? "Minor Crack" : health >= 10 ? "Critical Crack" : "Rupture";

    public static string MaintenanceStatus(float health)
        => health >= 99.99f ? "Routine monitoring" : health >= 60 ? "Monitor" :
            health >= 40 ? "Inspection required" : health >= 10 ? "Urgent inspection" : "Maintenance required";

    public static string Risk(float health)
    {
        if (health >= 80) return "healthy";
        if (health >= 60) return "watch";
        if (health >= 40) return "warning";
        if (health >= 10) return "critical";
        return "rupture";
    }

    public static string Action(float health)
    {
        if (health >= 80) return "Continue monitoring";
        if (health >= 60) return "Check joint at next planned inspection";
        if (health >= 40) return "Schedule joint inspection and check alignment";
        if (health >= 10) return "Inspect joint immediately; arrange controlled shutdown";
        return "Emergency stop; isolate conveyor and replace or repair splice";
    }

    // Prototype linear calibration, NOT an industrial standard or trained ML model.
    public static float Severity(float value, Vector2 limits)
        => Mathf.InverseLerp(limits.x, limits.y, value) * 100f;
}
