using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class bulletHoleDespawn : MonoBehaviour
{
    public int maxBulletDecals;
    private static Queue<GameObject> bulletDecals = new Queue<GameObject>();

    // When a new bullet decal is added we remove the oldest decal if queue is larger than the max
    void Start()
    {
        bulletDecals.Enqueue(this.gameObject);
        
        if(bulletDecals.Count > maxBulletDecals)
        {
            Destroy(bulletDecals.Dequeue());
        }
    }
}
