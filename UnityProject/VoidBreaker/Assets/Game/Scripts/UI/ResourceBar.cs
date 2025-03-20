using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceBar : MonoBehaviour
{
    public GameObject segmentPrefab;
    public int maxValue = 100;
    public float segmentSpacing = 5f;
    public int currentValue = 100;

    private GameObject[] segments;

    // Start is called before the first frame update
    void Start()
    {
        GenerateSegments();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateBar(currentValue);
        if (Input.GetKeyDown(KeyCode.R))
        {
            for (int i = 0; i < segments.Length; i++)
            {
                Destroy(segments[i]);
            }
            GenerateSegments();
        }
    }

    private void GenerateSegments()
    {
        segments = new GameObject[maxValue];
        float startX = -(maxValue / 2f) * segmentSpacing;

        for (int i = 0; i < maxValue; i++)
        {
            GameObject segment = Instantiate(segmentPrefab, transform);
            segments[i] = segment;
            RectTransform rect = segment.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(startX + (i * segmentSpacing), 0);
        }
    }

    public void UpdateBar(int currentValue)
    {
        for (int i = 0; i < maxValue; i++)
        {
            segments[i].SetActive(i < currentValue);
        }
    }
}
