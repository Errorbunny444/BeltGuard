// Dataset labels are visual inputs, never model inference or failure predictions.
public static class DamageReferenceProfile
{
    public static string Normalize(string label)
        => (label ?? "").Trim().ToLowerInvariant().Replace(' ', '_').Replace('-', '_');
    public static bool IsHole(string label)
    { string type = Normalize(label); return type == "small_hole" || type == "large_hole"; }
    public static bool IsWear(string label) => Normalize(label) == "surface_wear";
    public static bool IsSmall(string label)
    { string type = Normalize(label); return type == "small_hole" || type == "small_tear"; }
    public static string Advice(string label, float health)
    {
        // Preserve emergency priority. A visual label never overrides the stop interlock.
        if (health < 10) return MaintenanceAdvisor.Action(health);
        string type = Normalize(label);
        string next = health < 40 ? "Arrange controlled shutdown. " : "Schedule inspection. ";
        if (IsHole(type)) return next + "Inspect puncture, surrounding cover and reinforcement; check for trapped sharp material.";
        if (type == "small_tear" || type == "large_tear")
            return next + "Inspect tear ends and reinforcement; check for snagging or material impact.";
        if (type == "surface_wear") return health < 40
            ? next + "Inspect worn cover and reinforcement; check friction and loading contact."
            : "Monitor worn cover; inspect friction and loading contact at the next planned stop.";
        return MaintenanceAdvisor.Action(health);
    }
}
