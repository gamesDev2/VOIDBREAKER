using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorConsole  : BaseInteractable
{
    private bool mActivated = false;
    public bool Activated 
    { 
        get { return mActivated; } 
        private set { mActivated = value; }
    }

    public override void Interact(GameObject interactor)
    {
        mActivated = true;
    }

    public void ResetDoorControl()
    {
        mActivated = false;
    }
}
