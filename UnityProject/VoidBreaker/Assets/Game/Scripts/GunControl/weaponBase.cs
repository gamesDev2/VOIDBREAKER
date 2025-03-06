using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class weaponBase : MonoBehaviour
{
    [Header("Aim Vector")]
    public Transform playerCamera;

    public virtual void startAttack()
    {
        return;
    }
    public virtual void stopAttack()
    {
        return;
    }

}
