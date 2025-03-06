using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class weaponHandle : MonoBehaviour
{
    [Header("Hierarchy References")]
    public Camera playerCamera;

    [Header("Weapons")]
    [Tooltip("List of available weapons.")]
    public List<weaponBase> weapons = new List<weaponBase>();

    [Tooltip("Currently equipped weapon.")]
    public weaponBase equippedWeapon;

    [Header("Input")]
    // For non-melee weapons, Fire1 (left mouse) is used.
    // For melee weapons, we use Button2 (right mouse) to engage blade mode and Fire1 to slash.
    public KeyCode Button2 = KeyCode.Mouse1;

    // Private variable to keep track of the current weapon index.
    private int currentWeaponIndex = 0;

    void Start()
    {
        // Ensure the player's camera reference is assigned to each weapon.
        if (playerCamera != null)
        {
            // If a weapon is already assigned externally, use it.
            if (equippedWeapon != null)
            {
                equippedWeapon.playerCamera = playerCamera.transform;
            }
            // Otherwise, auto-equip the first weapon from the list.
            else if (weapons != null && weapons.Count > 0)
            {
                EquipWeapon(0);
            }
        }
    }

    void Update()
    {
        // Weapon swapping: Use number keys to equip a weapon.
        // Pressing "1" equips the first weapon, "2" the second, etc.
        for (int i = 0; i < weapons.Count; i++)
        {
            if (Input.GetKeyDown((i + 1).ToString()))
            {
                EquipWeapon(i);
            }
        }

        if (equippedWeapon != null)
        {
            if (equippedWeapon.isMeleeWeapon)
            {
                // Use right mouse button (Button2) to engage/disengage blade mode.
                if (Input.GetKeyDown(Button2))
                {
                    equippedWeapon.startAttack();  // Engage blade mode.
                }
                if (Input.GetKeyUp(Button2))
                {
                    equippedWeapon.stopAttack();   // Disengage blade mode.
                }
                // Use left mouse button to trigger a single slash only if blade mode is active.
                if (Input.GetButtonDown("Fire1"))
                {
                    // Cast to Sword so we can call its Slash() method.
                    Sword s = equippedWeapon as Sword;
                    if (s != null && s.isAttacking)
                    {
                        s.Slash();
                    }
                }
            }
            else
            {
                // For non-melee weapons, use "Fire1" for startAttack and stopAttack.
                if (Input.GetButtonDown("Fire1"))
                {
                    equippedWeapon.startAttack();
                }
                if (Input.GetButtonUp("Fire1"))
                {
                    equippedWeapon.stopAttack();
                }
            }
        }
    }

    /// <summary>
    /// Equips the weapon at the given index in the weapons list.
    /// </summary>
    public void EquipWeapon(int index)
    {
        if (index < 0 || index >= weapons.Count)
            return;

        // Deactivate currently equipped weapon if any.
        if (equippedWeapon != null)
        {
            equippedWeapon.gameObject.SetActive(false);
        }

        currentWeaponIndex = index;
        equippedWeapon = weapons[currentWeaponIndex];
        equippedWeapon.gameObject.SetActive(true);
        if (playerCamera != null)
        {
            equippedWeapon.playerCamera = playerCamera.transform;
        }
    }

    /// <summary>
    /// Equips the specified weapon.
    /// </summary>
    public void EquipWeapon(weaponBase newWeapon)
    {
        if (newWeapon == null)
            return;

        if (equippedWeapon != null)
        {
            equippedWeapon.gameObject.SetActive(false);
        }
        equippedWeapon = newWeapon;
        equippedWeapon.gameObject.SetActive(true);
        if (playerCamera != null)
        {
            equippedWeapon.playerCamera = playerCamera.transform;
        }
    }

    /// <summary>
    /// Unequips the current weapon.
    /// </summary>
    public void UnequipWeapon()
    {
        if (equippedWeapon != null)
        {
            equippedWeapon.playerCamera = null;
            equippedWeapon.gameObject.SetActive(false);
            equippedWeapon = null;
        }
    }
}
