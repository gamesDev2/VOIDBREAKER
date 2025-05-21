using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class weaponBase : MonoBehaviour
{
    [Header("Aim Vector")]
    public Transform playerCamera;

    [Header("Weapon Settings")]
    [Tooltip("Does this weapon handle its own input? (true for melee weapons)")]
    public bool isMeleeWeapon = false;
    public bool isSelectedWeapon = false;

    // Protected virtual methods so derived classes can override.
    protected virtual void Start()
    {
        // Base initialization if needed.
    }

    protected virtual void Update()
    {
        // If this is a melee weapon, let it handle its own input.
        if (isMeleeWeapon)
        {
            ProcessInput();
        }
    }

    // Melee weapons override this to process input.
    public virtual void ProcessInput() { }

    public virtual void startAttack() { }
    public virtual void stopAttack() { }
    public virtual void deselect() { }
    public virtual void select() { }
}
