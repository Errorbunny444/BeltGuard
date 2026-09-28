using System;
using UnityEngine;

// Wire field names deliberately match the Python/embedded team's JSON contract.
[Serializable]
public class BeltHealthData
{
    public float belt_speed, motor_current, vibration, temperature, encoder_position;
    public string joint_id;
    public bool damage_detected;
    public string damage_type;
    public float damage_severity, confidence, belt_position;
    public float health_score = -1; // Missing means calculate; zero is a valid rupture score.
    public string risk_level, maintenance_action;
    // Derived passport fields; older UDP senders do not need to supply these.
    public string current_condition, maintenance_status, inspection_status;
    public bool reset_demo; // Optional, only accepted when demo mode is enabled.
    public string last_updated; // Receipt time written locally, visible in the Inspector.
    public string data_source; // Optional: simulator or python_bridge; older packets remain supported.

    public static bool Finite(float v) => !float.IsNaN(v) && !float.IsInfinity(v);
    public bool IsValid()
    {
        return JointHealthManager.IsJointId(joint_id) &&
            Finite(belt_speed) && Finite(motor_current) && Finite(vibration) &&
            Finite(temperature) && Finite(encoder_position) && Finite(damage_severity) &&
            Finite(confidence) && Finite(belt_position) && Finite(health_score) &&
            belt_speed >= 0 && motor_current >= 0 && vibration >= 0 &&
            damage_severity >= 0 && damage_severity <= 100 && confidence >= 0 && confidence <= 1 &&
            health_score >= -1 && health_score <= 100;
    }

    public BeltHealthData Copy() => (BeltHealthData)MemberwiseClone();
}
