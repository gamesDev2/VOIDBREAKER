using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioAnimationPassOver : MonoBehaviour
{

    public Entity reference;

    public void PlayFootstepSound()
    {
        reference.PlayFootstepSound();
    }
}
