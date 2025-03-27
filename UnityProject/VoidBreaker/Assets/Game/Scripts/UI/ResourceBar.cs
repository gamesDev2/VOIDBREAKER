using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ResourceBar : MonoBehaviour
{
    public GameObject segmentPrefab;
    public int maxValue = 100;
    public float segmentSpacing = 5f;
    public int currentValue = 100;

    public float curveRadius = 100f;
    public float curveAngle = 180f;
    public bool isUpsideDown = false;

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
        float angleStep = curveAngle / (maxValue - 1);

        for (int i=0; i <maxValue; i++) 
        {
            GameObject segment = Instantiate(segmentPrefab, transform);
            segments[i] = segment;

            RectTransform rect = segment.GetComponent<RectTransform>();

            // Calculate angle per segment
            float angle = (-curveAngle / 2f + angleStep * i)-90f;

            float x = Mathf.Cos(angle * Mathf.Deg2Rad) * curveRadius;
            float y = Mathf.Sin(angle * Mathf.Deg2Rad) * curveRadius;

            if (!isUpsideDown)
            {
                y = -y;
                angle = 180f-angle; // Flip angle depending on bar type
            }

            // Convert angle to position
            

            rect.anchoredPosition = new Vector2(x, y);
            rect.localRotation = Quaternion.Euler(0, 0, angle-90); // Rotate to follow arc
        }
        

        /*segments = new GameObject[maxValue];
        float startX = -(maxValue / 2f) * segmentSpacing;

        for (int i = 0; i < maxValue; i++)
        {
            GameObject segment = Instantiate(segmentPrefab, transform);
            segments[i] = segment;
            RectTransform rect = segment.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(startX + (i * segmentSpacing), 0);
        }*/
    }

    public void UpdateBar(int currentValue)
    {
        for (int i = 0; i < maxValue; i++)
        {
            segments[i].SetActive(i < currentValue);
        }
    }
}
