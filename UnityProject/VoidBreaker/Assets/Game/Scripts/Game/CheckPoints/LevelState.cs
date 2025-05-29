using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelState
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

    
    public string convertLevelToJSON()
    {
        LevelWrapper level = new LevelWrapper();

        level.PlayerHealth = Player.Health;
        level.PlayerEnergy = Player.Energy;

        level.DeadEnemies = new bool[Enemies.Length];
        for (int i = 0; i < Enemies.Length; i++)
        {
            if (Enemies[i] == null)
            {
                level.DeadEnemies[i] = true;
            }
        }

        level.OpenDoors = new bool[Doors.Length];
        for (int i = 0; i < Doors.Length; i++)
        {
            if (Doors[i] == null)
            {
                level.OpenDoors[i] = true;
            }
        }

        level.ObjectiveCompleted = new bool[ObjectiveManager.objectives.Length];
        for (int i = 0; i <= ObjectiveManager.objectives.Length; i++)
        {
            level.ObjectiveCompleted[i] = ObjectiveManager.objectives[i].IsComplete;
        }

        level.checkPointIndex = CheckPointManager.checkPointIndex;

        return JsonUtility.ToJson(level);
    }



    [System.Serializable]
    private class LevelWrapper
    {
        public float PlayerHealth;
        public float PlayerEnergy;

        public bool[] DeadEnemies;
        public bool[] OpenDoors;
        
        public bool[] ObjectiveCompleted;

        public int checkPointIndex;
    }

}