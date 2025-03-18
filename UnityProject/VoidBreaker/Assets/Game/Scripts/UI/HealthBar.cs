using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    public GameObject segmentPrefab;
    public int maxHealth = 100;
    public float segmentSpacing = 1f;
    public int currentHealth = 100;

    private GameObject[] healthSegments;

    // Start is called before the first frame update
    void Start()
    {
        GenerateSegments();
    }

    // Update is called once per frame
    void Update()
    {
        UpdateHealth(currentHealth);
    }

    private void GenerateSegments()
    {
        healthSegments = new GameObject[maxHealth];
        float startX = -(maxHealth / 2f) * segmentSpacing;

        for (int i = 0; i<maxHealth; i++) 
        { 
            GameObject segment = Instantiate(segmentPrefab, transform);
            healthSegments[i] = segment;
            RectTransform rect = segment.GetComponent<RectTransform>();
            rect.anchoredPosition = new Vector2(startX +(i*segmentSpacing), 0);
        }
    }

    public void UpdateHealth(int currentHealth)
    {
        for (int i = 0; i<maxHealth; i++) 
        {
            healthSegments[i].SetActive(i < currentHealth);
        }
    }
}
