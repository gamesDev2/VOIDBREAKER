using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public void startDeathSequence()
    {
        Game_Manager.Instance.on_fade_to_black.Invoke(3f);
        Invoke("LoadDeathScreen", 4f);
    }

    private void LoadDeathScreen()
    {
        SceneManager.LoadScene("DeathScreen");
    }
}
