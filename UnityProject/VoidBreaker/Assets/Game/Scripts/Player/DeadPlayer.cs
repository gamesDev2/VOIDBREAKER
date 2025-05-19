using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeadPlayer : MonoBehaviour
{
    [Header("FPS Specific Settings")]
    [Tooltip("Assign the player's head transform (child of the player).")]
    public Transform head;
    public Quaternion cameraRotation;
    [Tooltip("Assign the player's Camera.")]
    public Camera playerCamera;

    void LateUpdate()
    {
        playerCamera.transform.position = head.position;
        playerCamera.transform.rotation = head.rotation;
    }
}
