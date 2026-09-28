using UnityEngine;

public class ConveyorPhysics : MonoBehaviour
{
    public ConveyorSpeedControl spc;
    public StartButton startButton;

    public Transform beltObject;

    private int orientation;

    void Start()
    {
        float rotY = transform.rotation.eulerAngles.y;

        if (rotY == 0 || rotY % 360 == 0)
            orientation = 0;
        else if (rotY == 90 || rotY % 360 == 90)
            orientation = 1;
        else if (rotY == 180 || rotY % 360 == 180)
            orientation = 2;
        else if (rotY == 270 || rotY % 360 == 270)
            orientation = 3;
    }

    void FixedUpdate()
    {
        // 🔥 CRITICAL CHECK
        if (startButton == null || !startButton.start) return;
        if (spc == null || beltObject == null) return;

        Vector3 direction = Vector3.right;

        if (orientation == 1) direction = Vector3.back;
        else if (orientation == 2) direction = Vector3.left;
        else if (orientation == 3) direction = Vector3.forward;

        beltObject.position += direction * spc.ConvSpeed * Time.fixedDeltaTime;
    }
}