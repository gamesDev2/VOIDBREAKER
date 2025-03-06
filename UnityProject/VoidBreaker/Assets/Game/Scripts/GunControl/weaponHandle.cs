using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class weaponHandle : MonoBehaviour
{
    [Header("Hierachy References")]
    public Camera playerCamera;
    public weaponBase weapon;

    [Header("Input")]
    public KeyCode shootButton = KeyCode.Mouse0;

    // Start is called before the first frame update
    void Start()
    {
        if (weapon != null)
        {
            weapon.playerCamera = playerCamera.transform;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(shootButton) && weapon != null)
        {
            weapon.startAttack();
        }

        if (Input.GetKeyUp(shootButton) && weapon != null)
        {
            weapon.stopAttack();
        }
    }

    public void gunSelection(weaponBase _newGun)
    {
        unequipGun();
        weapon = _newGun;
        weapon.playerCamera = playerCamera.transform;
    }

    // Can use this func to unequip all guns completely as well
    public void unequipGun()
    { 
        weapon.playerCamera = null;
        weapon = null;
    }
}
