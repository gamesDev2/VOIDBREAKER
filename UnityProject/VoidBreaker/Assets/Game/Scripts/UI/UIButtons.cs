using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIButtons : MonoBehaviour
{
    public TMP_Text text;
    public Image icon;

    public Color normalColor = Color.white;
    public Color hoverColor = Color.black;

    // Add sound variables here

    public void MouseHover()
    {
        text.color = hoverColor;

        if (icon != null)
        {
            icon.color = hoverColor;
        }
    }

    public void MouseExit()
    {
        text.color = normalColor;

        if (icon != null)
        {
            icon.color = normalColor;
        }
    }

    public void MousePressed()
    {
        text.color = hoverColor;

        if (icon != null)
        {
            icon.color = hoverColor;
        }
    }

    public void MouseReleased()
    {
        text.color = normalColor;

        if (icon != null)
        {
            icon.color = normalColor;
        }
        //Sound??
    }
}
