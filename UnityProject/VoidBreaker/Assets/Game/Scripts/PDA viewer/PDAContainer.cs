using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class PDAContainer : MonoBehaviour
{
    public PDAData[] data;
    [SerializeField] private string path;


    void Start()
    {
        if (path != null)
        {
            StreamReader dataReader = new StreamReader(path);
            string pdaJSON = dataReader.ReadToEnd();
            dataReader.Close();

            data = ParsePDAJSON(pdaJSON);
        }
    }

    private PDAData[] ParsePDAJSON(string json)
    {
        json = "{ \"data\":" + json + "}";
        PDAContainer container = JsonUtility.FromJson<PDAContainer>(json);
        return container.data;
    }
}
