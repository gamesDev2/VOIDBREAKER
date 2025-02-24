using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class gunHandle : MonoBehaviour
{
    [Header("Hierachy References")]
    public Camera playerCamera;
    public gunShooting gun;

    [Header("Input")]
    public KeyCode shootButton = KeyCode.Mouse0;

    // Start is called before the first frame update
    void Start()
    {
        if (gun != null)
        {
            gun.playerCamera = playerCamera;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(shootButton) && gun != null)
        {
            gun.startFire();
        }

        if (Input.GetKeyUp(shootButton) && gun != null)
        {
            gun.stopFire();
        }
    }

    public void updateGunSelection(gunShooting _newGun)
    {
        unequipGun();
        gun = _newGun;
        gun.playerCamera = playerCamera;
    }

    // Can use this func to unequip all guns completely as well
    public void unequipGun()
    { 
        gun.playerCamera = null;
        gun = null;
    }
}
