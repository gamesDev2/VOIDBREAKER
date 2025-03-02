using UnityEngine;

public class AttackAction : GOAPAction
{
    public float attackRange = 2f;      // distance at which we can deal damage
    public float attackDamage = 10f;    // how much damage we deal
    public float attackCooldown = 2f;   // time (seconds) between attacks

    private bool attacked = false;
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

        // If in attack range, deal damage
        if (dist <= attackRange)
        {
            // Check cooldown
            if (Time.time - lastAttackTime >= attackCooldown)
            {
                // Call the player's TakeDamage function
                Player_Controller playerEntity = player.GetComponent<Player_Controller>();
                if (playerEntity != null && playerEntity.GetHealth() > 10)
                {
                    Debug.Log(agentObj.name + " attacks player for " + attackDamage + " damage!");
                    playerEntity.TakeDamage(attackDamage);
                }

                lastAttackTime = Time.time;
                attacked = true;
            }
        }
        else
        {
            // Move closer
            goap.MoveTo(player.position);
        }

        return true;
    }

    public override bool IsDone()
    {
        return attacked;
    }

    public override bool RequiresInRange()
    {
        return false;
    }
}
