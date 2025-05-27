using System.Collections.Generic;
using UnityEngine;

public class CutObjectManager : MonoBehaviour
{
    public static CutObjectManager Instance { get; private set; }

    [Tooltip("Time (in seconds) after which a cut object is automatically destroyed.")]
    public float lifetime = 30f;

    [Tooltip("Maximum number of cut objects allowed. If exceeded, the oldest objects are destroyed.")]
    public int maxCutMeshCount = 50;

    // Internal record for each registered cut object.
    private Queue<CutObjectRecord> cutObjects = new Queue<CutObjectRecord>();

    private class CutObjectRecord
    {
        public GameObject obj;
        public float spawnTime;
    }

    private void Awake()
    {
        // Ensure the static instance is set.
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        float currentTime = Time.time;

        // Remove any expired objects (oldest first).
        while (cutObjects.Count > 0)
        {
            var record = cutObjects.Peek();
            if (record.obj == null)
            {
                cutObjects.Dequeue();
                continue;
            }
            if (currentTime - record.spawnTime > lifetime)
            {
                Destroy(record.obj);
                cutObjects.Dequeue();
            }
            else
            {
                // Since objects are stored in order, break as soon as we find one that hasn't expired.
                break;
            }
        }

        // If the total count exceeds the maximum, remove oldest objects.
        while (cutObjects.Count > maxCutMeshCount)
        {
            var record = cutObjects.Dequeue();
            if (record.obj != null)
            {
                Destroy(record.obj);
            }
        }
    }

    /// <summary>
    /// Registers a new cut object with the manager.
    /// </summary>
    /// <param name="cutObject">The newly created cut object.</param>
    public void RegisterCutObject(GameObject cutObject)
    {
        if (cutObject == null)
            return;

        CutObjectRecord record = new CutObjectRecord
        {
            obj = cutObject,
            spawnTime = Time.time
        };
        cutObjects.Enqueue(record);

        // Immediately enforce the maximum count if needed.
        while (cutObjects.Count > maxCutMeshCount)
        {
            var oldRecord = cutObjects.Dequeue();
            if (oldRecord.obj != null)
            {
                Destroy(oldRecord.obj);
            }
        }
    }
}
