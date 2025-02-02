﻿using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// A player script for disabling and demolishing buildings.
/// </summary>
public class BuildingDemolitionController : SerializableSingleton<BuildingDemolitionController>
{
    //Private Fields---------------------------------------------------------------------------------------------------------------------------------  

    //Serialized Fields----------------------------------------------------------------------------                                                    

    [SerializeField] private Camera camera;
    [SerializeField] private GameObject menu;
    [SerializeField] private Button enableDisableButton;
    [SerializeField] private TextMeshProUGUI enableDisableText;
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private Vector3 offsetFromHealthBar;
    [SerializeField] private Vector3 inactivePosition;

    //Non-Serialized Fields------------------------------------------------------------------------                                                    

    private bool demolishBuildingInput;
    private bool demolishBuilding;
    private bool showingDemolitionMenu;
    private LayerMask buildingsLayerMask;
    private Building selectedBuilding;
    private GraphicRaycaster graphicRaycaster;
    private int clickTimeout;

    //Public Properties------------------------------------------------------------------------------------------------------------------------------

    //Basic Public Properties----------------------------------------------------------------------

    /// <summary>
    /// The building the player has clicked on to show the demolition menu for.
    /// </summary>
    public Building SelectedBuilding { get => selectedBuilding; }

    //Initialization Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Awake() is run when the script instance is being loaded, regardless of whether or not the script is enabled. 
    /// Awake() runs before Start().
    /// </summary>
    protected override void Awake()
    {
        base.Awake();
        buildingsLayerMask = LayerMask.GetMask("Demolishable Building");
        graphicRaycaster = menu.GetComponent<GraphicRaycaster>();
        demolishBuildingInput = false;
    }

    //Core Recurring Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Update() is run every frame.
    /// </summary>
    private void Update()
    {
        GetInput();
        DemolishBuildings();
    }

    //Recurring Methods (Update())------------------------------------------------------------------------------------------------------------------  

    /// <summary>
    /// Gets the player's input from the keyboard and mouse / gamepad they're using.
    /// </summary>
    private void GetInput()
    {        
        if (InputManager.Instance.ButtonPressed("DemolishBuilding") == demolishBuildingInput)
        {
            demolishBuilding = false;
        }
        else
        {
            demolishBuildingInput = !demolishBuildingInput;
            demolishBuilding = demolishBuildingInput;
        }
    }

    /// <summary>
    /// Checks if the player wants to demolish or disable a building, and displays their options for that if they do.
    /// </summary>
    private void DemolishBuildings()
    {
        bool hitMenu = false;

        if (BuildingSpawnController.Instance.SpawningBuilding || MineralCollectionController.Instance.Mining)
        {
            if (showingDemolitionMenu)
            {
                HideDemolitionMenu();
            }
        }
        else if (demolishBuilding /*&& clickTimeout <= 0*/ && !MouseOverUI(out hitMenu))
        {           
            //Debug.Log($"BuildingDemolitionController.DemolishBuildings(), physics raycasting");
            RaycastHit hit;
            Ray ray = camera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out hit, Mathf.Infinity, buildingsLayerMask))
            {                
                Building building = hit.collider.GetComponentInParent<Building>();
                //Debug.Log($"BuildingDemolitionController.DemolishBuildings(), physics raycast hit, building is {building}, selectedBuilding is {selectedBuilding}");

                if (building == null || building == selectedBuilding)
                {
                    if (hitMenu)
                    {
                        return;
                    }
                }
                else if (building.BuildingType != EBuilding.Tower && (!showingDemolitionMenu || selectedBuilding != building))
                {
                    ShowDemolitionMenu(building);
                    return;
                }            
            }

            if (showingDemolitionMenu)
            {
                HideDemolitionMenu();
            }
        }
    }

    /// <summary>
    /// Checks if the mouse was over the general UI when the player clicked.
    /// </summary>
    /// <param name="hitMenu">Did the mouse click hit the demolition menu?</param>
    /// <returns>Whether the mouse was over the general UI when the player clicked.</returns>
    private bool MouseOverUI(out bool hitMenu)
    {
        //Debug.Log($"BuildingDemolitionController.MouseOverUI()");
        List<RaycastResult> results = new List<RaycastResult>();
        PointerEventData pointerEventData = new PointerEventData(eventSystem);

        hitMenu = false;
        pointerEventData.position = Input.mousePosition;
        graphicRaycaster.Raycast(pointerEventData, results);

        foreach (RaycastResult result in results)
        {
            //Debug.Log($"BuildingDemolitionController.MouseOverUI(), hit {result.gameObject}");
            switch (result.gameObject.tag)
            {
                case "Building Demolition Menu":
                    hitMenu = true;
                    break;
                default:
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Show building demolition options if the player has clicked on a building.
    /// </summary>
    private void ShowDemolitionMenu(Building building)
    {
        //Debug.Log($"BuildingDemolitionController.ShowDemolitionMenu()");
        selectedBuilding = building;
        showingDemolitionMenu = true;
        Vector3 pos = building.transform.position;
        menu.transform.position = new Vector3(pos.x, 1.5f, pos.z - 2f);
        enableDisableText.text = (building.DisabledByPlayer ? "Enable" : "Disable");
        menu.SetActive(true);
    }

    /// <summary>
    /// Hide building demolition options if the player has clicked away from a building or clicked a demolition option.
    /// </summary>
    public void HideDemolitionMenu()
    {
        //Debug.Log($"BuildingDemolitionController.HideDemolitionMenu()");
        selectedBuilding = null;
        showingDemolitionMenu = false;
        menu.SetActive(false);
        enableDisableButton.enabled = true;
        menu.transform.position = inactivePosition;
    }

    //Triggered Methods------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Enables or disables the building, depending on its current state.
    /// </summary>
    public void ToggleBuildingEnabled()
    {
        //Debug.Log($"BuildingDemolitionController.ToggleEnabled()");

        if (selectedBuilding.DisabledByPlayer)
        {
            selectedBuilding.DisabledByPlayer = false;
            selectedBuilding.Operational = true;
            enableDisableText.text = "Disable";
        }
        else
        {
            selectedBuilding.DisabledByPlayer = true;
            selectedBuilding.Operational = false;
            enableDisableText.text = "Enable";
        }

        HideDemolitionMenu();
    }

    /// <summary>
    /// Demolishes the building.
    /// </summary>
    public void DemolishBuilding()
    {
        //Debug.Log($"BuildingDemolitionController.DemolishBuilding()");
        BuildingFactory.Instance.Destroy(selectedBuilding, true, false);
        //HideDemolitionMenu(); //Called by BuildingFactory.Destroy()
    }
}
