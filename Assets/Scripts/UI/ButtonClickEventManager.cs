﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ButtonClickEventManager : MonoBehaviour
{
    [SerializeField] private EBuilding buildingType;

    private Button button;
    private bool clicked = false;

    public bool Clicked { get => clicked; }
    public EBuilding GetBuildingType { get => buildingType; }

    // Start is called before the first frame update
    void Awake()
    {
        button = GetComponent<Button>();        
    }

    // Update is called once per frame
    /*public void AssociatedKeyPressed()
    {
        if (Input.GetKeyDown(key)){
            FadeToColor(button.colors.pressedColor);
            button.onClick.Invoke();
        } else if (Input.GetKeyUp(key)) {
            FadeToColor(button.colors.normalColor);
        }
    }*/

    public void AfterUpdateCleanup()
    {
        if (clicked){
            clicked = false;
        }
    }

    /*private void FadeToColor(Color color){
        Graphic graphic = GetComponent<Graphic>();
        graphic.CrossFadeColor(color, button.colors.fadeDuration, true, true);
    }*/

    public void OnClick(){
        clicked = true;
    }
}