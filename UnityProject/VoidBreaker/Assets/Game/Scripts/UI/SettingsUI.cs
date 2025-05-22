using System.Collections;
using System.Collections.Generic;
using System.Transactions;
using UnityEngine;
using UnityEngine.UI;

public class SettingsUI : MonoBehaviour
{
    public GameObject settingsMainPanel;

    public GameObject graphicsSettings;

    public GameObject audioSettings;
    public Slider volumeSlider;

    public GameObject controlsSettings;

    private GameObject currentSubPanel;

    private GameObject returnToPanel;

    private void Start()
    {
        gameObject.SetActive(false);
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (currentSubPanel == null)
            {
                HideSettings();
            }
            else
            {
                BackToMainSettings();
            }
        }
    }

    public void ShowSettings(GameObject callerPanel)
    {
        returnToPanel = callerPanel;
        if (returnToPanel != null)
        {
            returnToPanel.SetActive(false);
        }
        gameObject.SetActive(true);
        settingsMainPanel.SetActive(true);
    }

    public void HideSettings()
    {
        settingsMainPanel.SetActive(false);
        gameObject.SetActive(false);

        if (returnToPanel != null)
        {
            returnToPanel.SetActive(true);
            returnToPanel = null;
        }
    }

    public void OpenSubPanel(GameObject subPanel)
    {
        settingsMainPanel.SetActive(false);
        if (currentSubPanel != null)
        {
            currentSubPanel.SetActive(false);
        }

        subPanel.SetActive(true);
        currentSubPanel = subPanel;
        Debug.Log(subPanel);
        if (subPanel == audioSettings)
        {
            Debug.Log(subPanel);
            float savedVolume = SettingsManager.Instance.userSettings.volume;
            volumeSlider.value = savedVolume;
        }

    }

    public void BackToMainSettings()
    {
        if (currentSubPanel != null)
        {
            currentSubPanel.SetActive(false);
            currentSubPanel = null;
        }
        settingsMainPanel.SetActive(true);
    }

    public void ApplySettings()
    {
       if (currentSubPanel == audioSettings) 
       {
            float newVolume = volumeSlider.value;
            SettingsManager.Instance.userSettings.volume = newVolume;
            SettingsManager.Instance.SaveSettings();
       }
    }

}
