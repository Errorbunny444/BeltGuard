using System;
using System.Collections.Generic;

// Standalone checks against the compiled project assembly and Unity's actual Mathf.
public static class DegradationModelChecks
{
    static void Check(bool ok, string message)
    {
        if (!ok) throw new Exception(message);
        Console.WriteLine("PASS: " + message);
    }
    public static void Main()
    {
        var model = new JointDegradationModel();
        var data = new BeltHealthData { health_score = 100 };
        model.Request(5);
        model.Step(data, .1f, 4);
        Check(data.health_score > 99 && data.health_score < 100, "Rupture request starts gradual degradation");
        var conditions = new HashSet<string> { "Healthy" };
        float previous = data.health_score;
        for (int i = 0; i < 400; i++)
        {
            model.Step(data, .1f, 4);
            CheckRange(data.health_score <= previous && data.health_score >= 0);
            conditions.Add(MaintenanceAdvisor.Condition(data.health_score));
            previous = data.health_score;
        }
        Check(conditions.Count == 5, "Healthy-to-rupture progression visits all five conditions");
        Check(data.health_score == 0 && data.damage_severity == 100, "Rupture ends at zero health and full damage");
        model.Request(1); model.Step(data, 100, 4);
        Check(data.health_score == 0, "Healthy target cannot repair a ruptured joint");
        float[] expected = {100,72,52,25};
        for (int stage = 1; stage <= 4; stage++)
        {
            data = new BeltHealthData { health_score = 100 };
            model.Request(stage);
            for (int i = 0; i < 400; i++) model.Step(data, .1f, 4);
            Check(Math.Abs(data.health_score - expected[stage-1]) < .001f, "Scenario " + stage + " settles at its target");
        }
        model.Request(5); model.Step(data, 0, 4);
        Check(data.health_score == 25, "Zero elapsed time preserves health");
        model.Request(2); model.Step(data, 100, 4);
        Check(data.health_score == 25, "Changing to a less severe target preserves accumulated damage");
        var untouched = new BeltHealthData { health_score = 100 };
        model.Request(5); model.Step(data, 1, 4);
        Check(untouched.health_score == 100, "Stepping one passport does not alter another joint");
    }
    static void CheckRange(bool ok) { if (!ok) throw new Exception("Health increased or became negative"); }
}
