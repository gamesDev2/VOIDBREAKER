using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MedKit : MonoBehaviour
{

    public float healthGiven = 40f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            Entity player = other.gameObject.GetComponent<Entity>();
            if (player.GetHealth() < 100)
            {
                player.TakeDamage(-healthGiven);
                Destroy(gameObject);
            }
        }
    }
}
