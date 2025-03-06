using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class weaponHandle : MonoBehaviour
{
    [Header("Hierarchy References")]
    public Camera playerCamera;
    public weaponBase weapon;

    [Header("Input")]
    public KeyCode Button1 = KeyCode.Mouse0;
    public KeyCode Button2 = KeyCode.Mouse1;

    void Start()
    {
        if (weapon != null)
        {
            weapon.playerCamera = playerCamera.transform;
        }
    }

    void Update()
    {
        // Only handle external input if the weapon is not melee.
        if (weapon != null && !weapon.isMeleeWeapon)
        {
            if (Input.GetKeyDown(Button1))
            {
                weapon.startAttack();
            }

            if (Input.GetKeyUp(Button1))
            {
                weapon.stopAttack();
            }
        }
    }

    public void gunEquip(weaponBase _newGun)
    {
        unequipGun();
        weapon = _newGun;
        weapon.playerCamera = playerCamera.transform;
    }

    public void unequipGun()
    {
        if (weapon != null)
        {
            weapon.playerCamera = null;
            weapon = null;
        }
    }
}
