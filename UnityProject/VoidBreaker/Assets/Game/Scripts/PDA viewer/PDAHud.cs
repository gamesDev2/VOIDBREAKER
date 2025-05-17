using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PDAHud : MonoBehaviour
{

    private int pageNumber;

    [Header("GUI handles")]
    [SerializeField] private TextMeshProUGUI pageNumberGui;
    [SerializeField] private TextMeshProUGUI Entry;
    [SerializeField] private Button nextPageButton;
    [SerializeField] private Button previousPageButton;
    [SerializeField] private Button exitViewerButton;

    void Start()
    {
        nextPageButton.onClick.AddListener(OnNextPage);
        previousPageButton.onClick.AddListener(OnPreviousPage);
        exitViewerButton.onClick.AddListener(OnExitPDA);
    }

    void OnEnable()
    {
        pageNumber = 1;
        pageNumberGui.text = "Page: " + pageNumber;
        Entry.pageToDisplay = pageNumber;
    }

    private void OnNextPage()
    {
        pageNumber++;
        pageNumberGui.text = "Page: " + pageNumber;
        Entry.pageToDisplay = pageNumber;
    }

    private void OnPreviousPage()
    {
        pageNumber = pageNumber <= 1 ? 1 : --pageNumber;
        pageNumberGui.text = "Page: " + pageNumber;
        Entry.pageToDisplay = pageNumber;
    }

    private void OnExitPDA()
    {
        Game_Manager.SetCursorLocked(true);
        gameObject.SetActive(false);
    }
}
