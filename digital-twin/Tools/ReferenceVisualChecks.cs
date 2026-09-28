using System;

// Label and recommendation contract checks; no model loading or inference.
public static class ReferenceVisualChecks
{
    static void Check(bool result, string name)
    { if (!result) throw new Exception(name); Console.WriteLine("PASS: " + name); }
    public static void Main()
    {
        Check(DamageReferenceProfile.IsHole("Large hole"), "COCO large-hole label");
        Check(DamageReferenceProfile.IsHole("Small hole"), "COCO small-hole label");
        Check(DamageReferenceProfile.IsSmall("Small Tear"), "COCO mixed-case small-tear label");
        Check(DamageReferenceProfile.Normalize("Large tear") == "large_tear", "COCO large-tear label");
        Check(DamageReferenceProfile.IsWear("surface_wear"), "Existing wear scenario remains supported");
        Check(!DamageReferenceProfile.IsHole("splice_crack"), "Existing splice crack is not treated as a hole");
        Check(!DamageReferenceProfile.IsHole(null), "Missing label is safe");
        foreach (var label in new[] {"Large hole", "Small hole", "Large tear", "Small Tear", "surface_wear"})
            Check(DamageReferenceProfile.Advice(label, 0) == MaintenanceAdvisor.Action(0), "Emergency priority: " + label);
        Check(DamageReferenceProfile.Advice("surface_wear",25).StartsWith("Arrange controlled shutdown"), "Critical wear cannot recommend monitoring only");
    }
}
