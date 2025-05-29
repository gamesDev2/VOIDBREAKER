using System.IO;
using UnityEngine;

public class PDAManager : MonoBehaviour
{
    public static PDAManager Instance { get; private set; }

    public static PDAData[] pdaEntries;
    public static bool[] CollectedEntries;
    [SerializeField] private static string path = "PDALogs/PDA";

    [SerializeField] private static TextAsset pdaDB;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        pdaDB = Resources.Load(path) as TextAsset;
        string json = pdaDB.ToString();

        ParsePDAJSON(json);
    }

    private static void ParsePDAJSON(string json)
    {
        pdaEntries = JsonUtility.FromJson<JSONWrapper<PDAData>>(json).data;
        CollectedEntries = new bool[pdaEntries.Length];
    }

    [System.Serializable]
    private class JSONWrapper<T>
    {
        public T[] data;
    }
}
