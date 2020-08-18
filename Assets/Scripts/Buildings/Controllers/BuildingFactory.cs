﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A factory class for buildings.
/// </summary>
public class BuildingFactory : Factory<BuildingFactory, Building, EBuilding>
{
    //Initialization Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Start() is run on the frame when a script is enabled just before any of the Update methods are called for the first time. 
    /// Start() runs after Awake().
    /// </summary>
    protected override void Start()
    {
        base.Start();

        foreach (List<Building> l in pool.Values)
        {
            foreach (Building b in l)
            {
                b.SetCollidersEnabled("Placement", false);
            }
        }
    }

    //Triggered Methods -----------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Get buildings of a specified type from BuildingFactory.
    /// </summary>
    /// <param name="buildingType">The type of building you want BuildingFactory to get for you.</param>
    /// <returns>A building of the specified type.</returns>
    public override Building Get(EBuilding buildingType)
    {
        Building building = base.Get(buildingType);
        building.Id = IdGenerator.Instance.GetNextId();
        building.Active = true;

        if (building.Terraformer != null)
        {
            EnvironmentalController.Instance.RegisterBuilding(building.Terraformer);
        }

        return building;
    }

    /// <summary>
    /// Custom modifications to a building after Get() retrieves one from the pool.
    /// </summary>
    /// <param name="result">The building being modified.</param>
    /// <returns>The modified building.</returns>
    protected override Building GetRetrievalSetup(Building result)
    {
        result.SetCollidersEnabled("Placement", true);
        return result;
    }

    /// <summary>
    /// Destroy a building.
    /// Note: it's probably better to call this method or another overload of Destroy() defined in BuildingFactory than Factory's version of destroy.
    /// </summary>
    /// <param name="building">The building to be destroyed.</param>
    /// <param name="consumingResources">Is the building consuming resources and does that consumption need to be cancelled now that it's being destroyed?</param>
    /// <param name="consumingResources">Was the building destroyed while placed, and therefore needs to leave behind foundations?</param>
    public void Destroy(Building building, bool consumingResources, bool killed)
    {
        BuildingController.Instance.DeRegisterBuilding(building);

        if (building.Terraformer != null)
        {
            EnvironmentalController.Instance.RemoveBuilding(building.Id);
        }

        if (consumingResources)
        {
            ResourceController.Instance.PowerConsumption -= building.PowerConsumption;
            ResourceController.Instance.WaterConsumption -= building.WaterConsumption;
            ResourceController.Instance.WasteConsumption -= building.WasteConsumption;
        }

        if (killed)
        {
            foreach (Vector3 offset in building.BuildingFoundationOffsets)
            {
                BuildingFoundationFactory.Instance.Get(building.transform.position + offset);
            }
        }

        building.Reset();
        Destroy(building.BuildingType, building);
    }
}
