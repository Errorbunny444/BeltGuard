using UnityEngine;
using System.Collections.Generic;

public class ConveyorObjectMove : MonoBehaviour
{
    [Header("References")]
    public ConveyorSpeedControl speedControl;
    public StartButton startButton;

    [Header("Motion")]
    [Tooltip("Must match the forward axis of the conveyor belt surface.")]
    public Vector3 direction = Vector3.forward;
    public float multiplier = 1f;

    private readonly HashSet<Rigidbody> touchingBodies = new HashSet<Rigidbody>();

    void Start()
    {
        if (speedControl == null)
        {
            speedControl = FindAnyObjectByType<ConveyorSpeedControl>();
        }

        if (startButton == null)
        {
            startButton = FindAnyObjectByType<StartButton>();
        }
    }

    void FixedUpdate()
    {
        if (speedControl == null)
        {
            speedControl = FindAnyObjectByType<ConveyorSpeedControl>();
        }

        if (startButton == null)
        {
            startButton = FindAnyObjectByType<StartButton>();
        }

        if (speedControl == null || startButton == null || !startButton.start)
        {
            return;
        }

        if (Mathf.Approximately(speedControl.ConvSpeed, 0f))
        {
            return;
        }

        Vector3 planarDirection = direction;
        planarDirection.y = 0f;

        if (planarDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            return;
        }

        Vector3 targetPlanarVelocity = planarDirection.normalized * (speedControl.ConvSpeed * multiplier);

        foreach (Rigidbody rb in touchingBodies)
        {
            if (rb == null || rb.isKinematic)
            {
                continue;
            }

            Vector3 targetVelocity = targetPlanarVelocity;
            targetVelocity.y = rb.linearVelocity.y;

            rb.WakeUp();
            rb.linearVelocity = targetVelocity;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        TryRegisterBody(collision.rigidbody);
    }

    void OnCollisionStay(Collision collision)
    {
        TryRegisterBody(collision.rigidbody);
    }

    void OnCollisionExit(Collision collision)
    {
        Rigidbody rb = collision.rigidbody;
        if (rb != null)
        {
            touchingBodies.Remove(rb);
        }
    }

    private void TryRegisterBody(Rigidbody rb)
    {
        if (rb == null || rb.isKinematic)
        {
            return;
        }

        if (rb.GetComponent<BoltIdentity>() == null)
        {
            return;
        }

        touchingBodies.Add(rb);
    }
}
