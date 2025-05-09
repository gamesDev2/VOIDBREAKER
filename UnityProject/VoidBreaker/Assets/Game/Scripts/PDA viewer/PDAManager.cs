using System.IO;
using UnityEngine;

public class PDAManager : MonoBehaviour
{
    public static PDAManager instance;

    public static PDAData[] pdaEntries;
    [SerializeField] private static string path = "Assets/Game/PDALogs/PDA.json";

    void Awake()
    {
        StreamReader sr = new StreamReader(path);

        string json = sr.ReadToEnd();
        sr.Close();

        ParsePDAJSON(json);
    }

    private static void ParsePDAJSON(string json)
    {
        pdaEntries = JsonUtility.FromJson<JSONWrapper<PDAData>>(json).data;
    }

    [System.Serializable]
    private class JSONWrapper<T>
    {
        public T[] data;
    }
}
