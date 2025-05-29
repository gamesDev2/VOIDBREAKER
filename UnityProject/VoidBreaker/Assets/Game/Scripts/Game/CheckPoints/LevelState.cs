using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelState : MonoBehaviour
{
    [Header("Level Elements to keep track of")]
    [Tooltip("The Player entity component")]
    public Entity Player;
    [Tooltip("All enemies entity components in the level")]
    public Entity[] Enemies;
    [Tooltip("All Security Doors in the level")]
    public SecurityDoor[] Doors;
    [Tooltip("The Objective Manager object")]
    public ObjectiveManager ObjectiveManager;
    [Tooltip("HUD Controller to add PDA entries back in")]
    public HUDController HUD;

    public string convertLevelToJSON()
    {
        // Constructing Level state wrapper for conversion into JSON
        LevelWrapper level = new LevelWrapper();

        // Noting player health and energy
        level.PlayerHealth = Player.Health;
        level.PlayerEnergy = Player.Energy;

        // Counting which enemies are dead and which are alive
        level.DeadEnemies = new bool[Enemies.Length];
        for (int i = 0; i < Enemies.Length; i++)
        {
            if (Enemies[i] == null)
            {
                level.DeadEnemies[i] = true;
            }
        }

        // Checking which doors have been opened
        level.OpenDoors = new bool[Doors.Length];
        for (int i = 0; i < Doors.Length; i++)
        {
            if (Doors[i].open)
            {
                level.OpenDoors[i] = true;
            }
        }

        // Checking completed objectives
        level.ObjectiveCompleted = new bool[ObjectiveManager.objectives.Length];
        for (int i = 0; i < ObjectiveManager.objectives.Length; i++)
        {
            level.ObjectiveCompleted[i] = ObjectiveManager.objectives[i].IsComplete;
        }

        // Fetching which PDA entries are completed
        level.pdaEntriesCollected = PDAManager.CollectedEntries;

        // Checking which checkpoint the player has reached
        level.checkPointIndex = CheckPointManager.checkPointIndex;

        return JsonUtility.ToJson(level);
    }

    public void convertJsonToLevel(string JSON)
    {
        LevelWrapper level = JsonUtility.FromJson<LevelWrapper>(JSON);

        Player.Health = level.PlayerHealth;
        Player.Energy = level.PlayerEnergy;

        Transform spawn = CheckPointManager.checkPoints[level.checkPointIndex].transform;
        Player.gameObject.transform.position = spawn.position;
        Player.gameObject.transform.rotation = spawn.rotation;

        for (int i = 0; i < level.DeadEnemies.Length; i++)
        {
            if (level.DeadEnemies[i])
            {
                Enemies[i].Health = 0;
                Enemies[i].Die();
            }
        }

        for (int i = 0; i < level.OpenDoors.Length; i++)
        {
            if (level.OpenDoors[i])
            {
                foreach(DoorConsole c in Doors[i].doorConsoles)
                {
                    c.Activated = true;
                }
            }
        }

        Game_Manager.Instance.on_door_console_update.Invoke();

        for (int i = 0; i < level.ObjectiveCompleted.Length; i++)
        {
            if (level.ObjectiveCompleted[i])
            {
                ObjectiveManager.objectives[i].ForceCompleteObjective();
            }
        }

        for (int i = 0; i < level.pdaEntriesCollected.Length; i++)
        {
            PDAManager.CollectedEntries[i] = level.pdaEntriesCollected[i];

            if (level.pdaEntriesCollected[i])
            {
                PDAData data = PDAManager.pdaEntries[i];
                HUD.AddOrUpdateEntry(data.Title, data.Entry);
            }
        }
    }

    [System.Serializable]
    public class LevelWrapper
    {
        public float PlayerHealth;
        public float PlayerEnergy;

        public bool[] DeadEnemies;
        public bool[] OpenDoors;
        
        public bool[] ObjectiveCompleted;
        public bool[] pdaEntriesCollected;

        public int checkPointIndex;
    }
}