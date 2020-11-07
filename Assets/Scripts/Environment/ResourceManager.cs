﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Rewired;

/// <summary>
/// A manager class for resource gathering and usage.
/// </summary>
public class ResourceManager : PublicInstanceSerializableSingleton<ResourceManager>
{
    //Private Fields---------------------------------------------------------------------------------------------------------------------------------  

    //Serialized Fields----------------------------------------------------------------------------                                                    

    [Header("Resource Supplies")]
    [SerializeField] private int ore;
    [SerializeField] private int powerSupply;
    [SerializeField] private int plantsSupply;
    [SerializeField] private int waterSupply;
    [SerializeField] private int gasSupply;

    [Header("Resource Consumption")]
    [SerializeField] private int powerConsumption;
    [SerializeField] private int plantsConsumption;
    [SerializeField] private int waterConsumption;
    [SerializeField] private int gasConsumption;

    [Header("Testing")]
    [SerializeField] private bool developerResourcesEnabled;
    [SerializeField] private int developerResources;

    //Non-Serialized Fields------------------------------------------------------------------------                                                    

    //Resource Consumption

    //Resource Availability
    private bool powerAvailable = false;
    private bool plantsAvailable = false;
    private bool waterAvailable = false;
    private bool gasAvailable = false;
    private Player playerInputManager;

	public UnityAction resourcesUpdated;

    //Complex Public Properties--------------------------------------------------------------------                                                    

    /// <summary>
    /// How much gas the player is consuming per second.
    /// </summary>
    public int GasConsumption
    {
        get
        {
            return gasConsumption;
        }

        set
        {
            gasConsumption = value;
			resourcesUpdated?.Invoke();
			CheckResourceSupply();
        }
    }

    /// <summary>
    /// How much gas the player is generating per second.
    /// </summary>
    public int GasSupply
    {
        get
        {
            return gasSupply;
        }

        set
        {
            gasSupply = value;
			resourcesUpdated?.Invoke();
			CheckResourceSupply();
        }
    }

    /// <summary>
    /// How much ore the player has collected
    /// </summary>
    public int Ore
    {
        get
        {
            return ore;
        }

        set
        {
            ore = value;
            resourcesUpdated?.Invoke();
            CheckResourceSupply();
        }
    }

    /// <summary>
    /// How much power the player is consuming per second.
    /// </summary>
    public int PowerConsumption
    {
        get
        {
            return powerConsumption;
        }

        set
        {
            powerConsumption = value;
			resourcesUpdated?.Invoke();
			CheckResourceSupply();
        }
    }

    /// <summary>
    /// How much power the player is generating per second.
    /// </summary>
    public int PowerSupply
    {
        get
        {
            return powerSupply;
        }

        set
        {
            powerSupply = value;
			resourcesUpdated?.Invoke();
			CheckResourceSupply();
        }
	}

	/// <summary>
	/// How much power the player is consuming per second.
	/// </summary>
	public int PlantsConsumption
	{
		get
		{
			return plantsConsumption;
		}

		set
		{
			plantsConsumption = value;
			resourcesUpdated?.Invoke();
			CheckResourceSupply();
		}
	}

	/// <summary>
	/// How much plants the player is collecting per second.
	/// </summary>
	public int PlantsSupply
	{
		get
		{
			return plantsSupply;
		}

		set
		{
			plantsSupply = value;
			resourcesUpdated?.Invoke();
			CheckResourceSupply();
		}
	}

	/// <summary>
	/// How much water the player is consuming per second.
	/// </summary>
	public int WaterConsumption
    {
        get
        {
            return waterConsumption;
        }

        set
        {
            waterConsumption = value;
			resourcesUpdated?.Invoke();
			CheckResourceSupply();
        }
    }

    /// <summary>
    /// How much water the player is collecting per second.
    /// </summary>
    public int WaterSupply
    {
        get
        {
            return waterSupply;
        }

        set
        {
            waterSupply = value;
			resourcesUpdated?.Invoke();
			CheckResourceSupply();
        }
	}

	/// <summary>
	/// How much gas the player has to spare.
	/// </summary>
	public int SurplusGas
	{
		get
		{
			return gasSupply - gasConsumption;
		}
	}

	/// <summary>
	/// How much power the player has to spare.
	/// </summary>
	public int SurplusPower
	{
		get
		{
			return powerSupply - powerConsumption;
		}
	}

	/// <summary>
	/// How much plants the player has to spare.
	/// </summary>
	public int SurplusPlants
	{
		get
		{
			return plantsSupply - plantsConsumption;
		}
	}

	/// <summary>
	/// How much water the player has to spare.
	/// </summary>
	public int SurplusWater
	{
		get
		{
			return waterSupply - waterConsumption;
		}
	}

	//Initialization Methods-------------------------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Start() is run on the frame when a script is enabled just before any of the Update methods are called for the first time. 
	/// Start() runs after Awake().
	/// </summary>
	private void Start()
    {
        playerInputManager = POD.Instance.PlayerInputManager;
    }

    //Core Recurring Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Update() is run every frame.
    /// </summary>
    private void Update()
    {
        if (!PauseMenuManager.Paused)
        {
            CheckDeveloperResources();
            CheckResourceSupply();
        }
    }

    //Recurring Methods (Update())------------------------------------------------------------------------------------------------------------------  

    /// <summary>
    /// Checks if the developer has requested extra resources in the inspector, and grants them one batch of them if so.
    /// </summary>
    private void CheckDeveloperResources()
    {
        if (Application.isEditor && developerResourcesEnabled)
        {
            if (playerInputManager.GetButtonDown("Get Developer Resources"))
            {
                Ore += developerResources;
                PowerSupply += developerResources;
                PlantsSupply += developerResources;
                WaterSupply += developerResources;
                GasSupply += developerResources;
            }
            else if (playerInputManager.GetButtonDown("Remove Developer Resources"))
            {
                Ore = Mathf.Max(0, ore - developerResources);
                PowerSupply = Mathf.Max(0, powerSupply - developerResources);
                PlantsSupply = Mathf.Max(0, plantsSupply - developerResources);
                WaterSupply = Mathf.Max(0, waterSupply - developerResources);
                GasSupply = Mathf.Max(0, gasSupply - developerResources);
            }
        }

    }

    /// <summary>
    /// Check if there is sufficient or insufficient supplies of either resource, and shutdown and restore buildings appropriately.
    /// </summary>
    private void CheckResourceSupply()
    {
        //Get initial values
        bool initialPowerStatus = powerAvailable;
        bool initialWaterStatus = waterAvailable;
        bool initialPlantsStatus = plantsAvailable;
        bool initialGasStatus = gasAvailable;
        //Debug.Log($"ResourceManager.CheckResourceSupply() starting, power available: {powerAvailable}, water available: {waterAvailable}, plants available: {plantsAvailable}, gas available: {gasAvailable}");

        //Check if power needs to be updated
        if ((powerAvailable && powerSupply < powerConsumption) || (powerAvailable && powerSupply == 0) || (!powerAvailable && powerSupply >= powerConsumption && powerSupply != 0))
        {
            powerAvailable = !powerAvailable;
        }

        //Check if water needs to be updated
        if ((waterAvailable && waterSupply < waterConsumption) || (waterAvailable && waterSupply == 0) || (!waterAvailable && waterSupply >= waterConsumption && waterSupply != 0))
        {
            waterAvailable = !waterAvailable;
        }

        //Check if waste needs to be updated
        if ((plantsAvailable && plantsSupply < plantsConsumption) || (plantsAvailable && plantsSupply == 0) || (!plantsAvailable && plantsSupply >= plantsConsumption && plantsSupply != 0))
        {
            plantsAvailable = !plantsAvailable;
        }

        //Check if gas needs to be updated
        if ((gasAvailable && gasSupply < gasConsumption) ||( gasAvailable && gasSupply == 0) || (!gasAvailable && gasSupply >= gasConsumption && gasSupply != 0))
        {
            gasAvailable = !gasAvailable;
        }

        //Debug.Log($"ResourceManager.CheckResourceSupply() checked if resource availability changed, power available: {powerAvailable} (was {initialPowerStatus}), water available: {waterAvailable} (was {initialWaterStatus}), plants available: {plantsAvailable} (was {initialPlantsStatus}), gas available: {gasAvailable} (was {initialGasStatus})");

        //Check if there's been a change
        if (initialPowerStatus != powerAvailable || initialWaterStatus != waterAvailable || initialPlantsStatus != plantsAvailable || initialGasStatus != gasAvailable)
        {
            //Check if buildings need to be shutdown
            if (!powerAvailable || !waterAvailable || !plantsAvailable || !gasAvailable)
            {
                //Debug.Log("Shutdown Buildings.");
                BuildingManager.Instance.ShutdownBuildings(powerAvailable, waterAvailable, plantsAvailable, gasAvailable);
            }

            //Check if buildings can be restored
            if (powerAvailable || waterAvailable || plantsAvailable || gasAvailable)
            {
                //Debug.Log("Restore Buildings");
                BuildingManager.Instance.RestoreBuildings(powerAvailable, waterAvailable, plantsAvailable, gasAvailable);
            }
		}
    }
}
