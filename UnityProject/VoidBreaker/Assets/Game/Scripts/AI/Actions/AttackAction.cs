using UnityEngine;

public class AttackAction : GOAPAction
{
    public float attackRange = 2f;      // distance at which we can deal damage
    public float attackDamage = 10f;    // how much damage we deal
    public float attackCooldown = 2f;   // time (seconds) between attacks

    private float lastAttackTime = -999f;
    private Transform player;

    void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    public override bool CheckProceduralPrecondition(GameObject agent)
    {
        // We can only attack if we have a reference to the player
        return (player != null);
    }

    public override bool Perform(GameObject agentObj)
    {
        if (player == null) return false;

        GOAPAgent goap = agentObj.GetComponent<GOAPAgent>();
        if (!goap) return false;

        float dist = Vector3.Distance(agentObj.transform.position, player.position);

        // Get player's health
        Player_Controller playerEntity = player.GetComponent<Player_Controller>();
        if (playerEntity == null) return false;

        // If player's health is already <= 10, no more attacking
        if (playerEntity.GetHealth() <= 10f)
        {
            // Do nothing, but the action will end soon because IsDone() sees health <= 10
            return true;
        }

        // Otherwise, if in attack range, deal damage (respecting cooldown)
        if (dist <= attackRange)
        {
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                Debug.Log(agentObj.name + " attacks player for " + attackDamage + " damage!");
                playerEntity.TakeDamage(attackDamage);

                lastAttackTime = Time.time;
            }
        }
        else
        {
            // Move closer if out of range
            goap.MoveTo(player.position);
        }

        return true; // Action is still ongoing until player's health <= 10
    }

    /// <summary>
    /// The action ends when the player's health is at or below 10.
    /// </summary>
    public override bool IsDone()
    {
        if (player == null) return true; // No player => can't attack
        Player_Controller playerEntity = player.GetComponent<Player_Controller>();
        if (playerEntity == null) return true; // No health script => end

        // Stop attacking once player's health <= 10
        return (playerEntity.GetHealth() <= 10f);
    }

    public override bool RequiresInRange()
    {
        // We handle range checks inside Perform(), so set this to false
        return false;
    }
}
