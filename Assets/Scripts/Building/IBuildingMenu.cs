using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using BuildingType = System.Int32;
public interface IBuildingMenu
{
    void Show();
    void Hide();
    void OnBuildingSelected(int buildingIndex);
    void Initialize(Action<BuildingType> onBuildingSelected);
}
