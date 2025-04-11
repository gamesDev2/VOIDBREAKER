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

    void Start()
    {
        player = GetComponent<Entity>();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleShift))
        {
            phaseShiftActive = !phaseShiftActive;

            if (phaseShiftActive)
            {
                SetPhaseShiftActive(true);
                if (Game_Manager.Instance != null)
                {
                    Game_Manager.Instance.on_mesh_trail.Invoke(true);
                }
            }
            else
            {
                SetPhaseShiftActive(false);
                if (Game_Manager.Instance != null)
                {
                    Game_Manager.Instance.on_mesh_trail.Invoke(false);
                }
            }
        }

        if (player != null && player.GetEnergy() <= 0.0f)
        {
            SetPhaseShiftActive(false);
            if (Game_Manager.Instance != null)
            {
                Game_Manager.Instance.on_mesh_trail.Invoke(false);
            }
        }
    }

    private void SetPhaseShiftActive(bool active)
    {
        if (active)
        {
            Time.timeScale = timeSlow;
            player.timeFlow = 1.0f / timeSlow;
        }
        else
        {
            Time.timeScale = 1.0f;
            player.timeFlow = 1.0f;
        }

        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        player.SetSpecialModeActive(active);
    }
}
