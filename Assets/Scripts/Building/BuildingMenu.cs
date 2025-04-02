using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using BuildingType = System.Int32;
public class BuildingMenu : MonoBehaviour, IBuildingMenu
{
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private Button[] buildingButtons;

    private Action<int> onBuildingSelected;

    private void Start()
    {
        Hide(); // Ensure the menu is hidden at the start
    }

    public void Initialize(Action<BuildingType> onBuildingSelected)
    {
        this.onBuildingSelected = onBuildingSelected; // Store the callback
        for (int i = 0; i < buildingButtons.Length; i++)
        {
            int index = i; // Capture the current index
            buildingButtons[i].onClick.AddListener(() => OnBuildingButtonClicked(index)); // Assign button click listener
        }
    }

    private void OnBuildingButtonClicked(int index)
    {
        onBuildingSelected?.Invoke(index); // Invoke the callback with the selected index
        Hide(); // Optionally hide the menu after selection
    }

    public void Show()
    {
        menuPanel.SetActive(true);
    }

    public void Hide()
    {
        menuPanel.SetActive(false);
    }

    public void OnBuildingSelected(int buildingIndex)
    {
        onBuildingSelected?.Invoke(buildingIndex);
        Hide();
    }

    public bool IsActive()
    {
        return menuPanel.activeSelf; // Return true if the menu is active
    }
}