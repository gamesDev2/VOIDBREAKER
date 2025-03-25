using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class phaseShift : MonoBehaviour
{
    public KeyCode toggleShift = KeyCode.F;

    public float timeSlow = 0.1f;

    private bool phaseShiftActive = false;
    private Entity player;

    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<Entity>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(toggleShift))
        {
            phaseShiftActive = !phaseShiftActive;

            if (phaseShiftActive)
            {
                Time.timeScale = timeSlow;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                player.timeFlow = 1.0f / timeSlow;
                if (Game_Manager.Instance != null)
                {
                    Game_Manager.Instance.on_mesh_trail.Invoke(true);
                }
            }
            else
            {
                if (Game_Manager.Instance != null)
                {
                    Game_Manager.Instance.on_mesh_trail.Invoke(false);
                }
                Time.timeScale = 1.0f;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
                player.timeFlow = 1.0f;
            }
        }
    }
}
