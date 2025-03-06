using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class weaponBase : MonoBehaviour
{
    [Header("Aim Vector")]
    public Transform playerCamera;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public virtual void startAttack()
    {
        return;
    }
    public virtual void stopAttack()
    {
        return;
    }

}
