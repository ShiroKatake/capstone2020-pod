﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A controller class for tracking which parts of the map have buildings, can be spawned to by aliens, etc.
/// </summary>
public class MapController : MonoBehaviour
{
    //Private Fields---------------------------------------------------------------------------------------------------------------------------------  

    //Serialized Fields----------------------------------------------------------------------------                                                    

    [Header("Map Size")]
    [SerializeField] private int xMax;
    [SerializeField] private int zMax;

    [Header("No Alien Spawning Area")]
    [SerializeField] private Vector3 innerBottomLeft;
    [SerializeField] private Vector3 innerTopRight;

    //Non-Serialized Fields------------------------------------------------------------------------                                                    
    
    [Header("Testing")]
    private bool[,] availableBuildingPositions;
    private bool[,] alienExclusionArea;
    private bool[,] availableAlienPositions;

    [SerializeField] private List<Vector3> alienSpawnablePositions;

    //Public Properties------------------------------------------------------------------------------------------------------------------------------

    //Singleton Public Property--------------------------------------------------------------------                                                    

    /// <summary>
    /// MapController's singleton public property.
    /// </summary>
    public static MapController Instance { get; protected set; }

    //Initialization Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Awake() is run when the script instance is being loaded, regardless of whether or not the script is enabled. 
    /// Awake() runs before Start().
    /// </summary>
    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There should never be more than one MapController.");
        }

        Instance = this;
        availableBuildingPositions = new bool[xMax + 1 , zMax + 1];
        availableAlienPositions = new bool[xMax + 1, zMax + 1];
        alienExclusionArea = new bool[xMax + 1, zMax + 1];
        alienSpawnablePositions = new List<Vector3>();
    }

    private void Start()
    {
        float alienHoverHeight = AlienFactory.Instance.AlienHoverHeight;
        int noAlienXMin = (int)Mathf.Round(innerBottomLeft.x);
        int noAlienXMax = (int)Mathf.Round(innerTopRight.x);
        int noAlienZMin = (int)Mathf.Round(innerBottomLeft.z);
        int noAlienZMax = (int)Mathf.Round(innerTopRight.z);

        //Debug.Log($"Enemies cannot spawn within ({noAlienXMin}, {noAlienXMax}) to ({noAlienZMin}, {noAlienZMax})");

        for (int i = 0; i <= xMax; i++)
        {
            for (int j = 0; j <= zMax; j++)
            {
                //Debug.Log($"Assessing position ({i},{j})");
                availableBuildingPositions[i, j] = true;
                availableAlienPositions[i, j] = ((i < noAlienXMin || i > noAlienXMax) && (j < noAlienZMin || j > noAlienZMax));
                alienExclusionArea[i, j] = !availableAlienPositions[i, j];

                //Debug.Log($"available for building: {availableBuildingPositions[i, j]}, available for enemies: {availableAlienPositions[i, j]}, alien exclusion area: {alienExclusionArea[i, j]}");

                if (availableAlienPositions[i, j])
                {
                    alienSpawnablePositions.Add(new Vector3(i, alienHoverHeight, j));
                }
            }
        }
    }

    //Triggered Methods------------------------------------------------------------------------------------------------------------------------------

    //Availabile Position Methods------------------------------------------------------------------

    /// <summary>
    /// Checks if a given building can legally be placed given its size and position and the spaces available.
    /// </summary>
    /// <param name="building">The building whose placement is being checked.</param>
    /// <returns>Whether the building can legally be placed.</returns>
    public bool PositionAvailableForBuilding(Building building)
    {
        Vector3 buildingPos = building.transform.position;
        //Debug.Log($"Verifying for building at {buildingPos}");

        foreach (Vector3 offset in building.BuildingFoundationOffsets)
        {
            if (!PositionAvailableForSpawning(buildingPos + offset, false))
            {
                Debug.Log("MapController.PositionAvailableForBuilding returned false");
                return false;
            }
        }

        Debug.Log("MapController.PositionAvailableForBuilding returned false");
        return true;
    }

    /// <summary>
    /// Checks if something can legally be spawned at a given position given the spaces available.
    /// </summary>
    /// <param name="position">The position the something would be spawned at.</param>
    /// <param name="alien">Whether or not the something is an alien.</param>
    /// <returns>Whether something can legally be spawned.</returns>
    public bool PositionAvailableForSpawning(Vector3 position, bool alien)
    {
        //Debug.Log($"Verifying for spawnable at {position}");
        position.x = Mathf.Round(position.x);
        position.z = Mathf.Round(position.z);

        if (position.x < 0 || position.x > xMax || position.z < 0 || position.z > zMax)
        {
            Debug.Log($"Can't spawn at {position}, which is outside the bounds of (0,0) to ({xMax},{zMax})");
            return false;
        }

        if (alien && alienExclusionArea[(int)position.x, (int)position.z])
        {
            Debug.Log($"Can't spawn an alien at {position}, which is within the alien exclusion area.");
        }

        if (!availableBuildingPositions[(int)position.x, (int)position.z])
        {
            Debug.Log($"Can't spawn at {position}, which is already occupied by a building.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Gets a random position that an alien could legally be spawned at.
    /// </summary>
    /// <returns>A position for an alien to spawn at.</returns>
    public Vector3 RandomAlienSpawnablePos(List<Vector3> temporarilyUnavailablePositions)
    {
        List<Vector3> availablePositions = new List<Vector3>(alienSpawnablePositions);

        foreach (Vector3 p in temporarilyUnavailablePositions)
        {
            if (availablePositions.Contains(p))
            {
                availablePositions.Remove(p);
            }
        }

        Debug.Log($"Getting alien spawnable position, available positions: {availablePositions.Count}");

        switch (availablePositions.Count)
        {
            case 0:
                return new Vector3 (-1, 0.5f, -1);
            case 1:
                return availablePositions[0];
            default:
                return availablePositions[Random.Range(0, availablePositions.Count)];
        }
    }

    //Entity Registration Methods------------------------------------------------------------------

    /// <summary>
    /// Registers a building with MapController so that it knows that the spaces it occupies are occupied.
    /// </summary>
    /// <param name="building">The building to be registered.</param>
    public void RegisterBuilding(Building building)
    {
        foreach (Vector3 offset in building.BuildingFoundationOffsets)
        {
            UpdateAvailablePosition(building.gameObject, building.transform.position + offset, false);
        }
    }
    
    /// <summary>
    /// Registers a mineral with MapController so that it knows that the space it occupies is occupied.
    /// </summary>
    /// <param name="mineral">The mineral to be registered.</param>
    public void RegisterMineral(Mineral mineral)
    {
        UpdateAvailablePosition(mineral.gameObject, mineral.transform.position, false);
    }
    
    /// <summary>
    /// Deregisters a building with MapController so that it knows that the spaces it occupied are unoccupied.
    /// </summary>
    /// <param name="building">The building to be deregistered.</param>
    public void DeRegisterBuilding(Building building)
    {
        foreach (Vector3 offset in building.BuildingFoundationOffsets)
        {
            UpdateAvailablePosition(building.gameObject, building.transform.position + offset, true);
        }
    }
    
    /// <summary>
    /// Deregisters a mineral with MapController so that it knows that the space it occupied is unoccupied.
    /// </summary>
    /// <param name="mineral">The mineral to be deregistered.</param>
    public void DeRegisterMineral(Mineral mineral)
    {
        UpdateAvailablePosition(mineral.gameObject, mineral.transform.position, true);
    }

    /// <summary>
    /// Updates the availability of the space(s) occupied / to be occupied by a building or mineral.
    /// </summary>
    /// <param name="gameObject">The game object whose space(s) are having their availability updated.</param>
    /// <param name="position">The position having its availability updated.</param>
    /// <param name="available">Is the space now available, or is it now unavailable?</param>
    private void UpdateAvailablePosition(GameObject gameObject, Vector3 position, bool available)
    {
        int x = (int)Mathf.Round(position.x);
        int z = (int)Mathf.Round(position.z);

        if (x >= 0 && x <= xMax && z >= 0 && z <= zMax)
        {
            Debug.Log($"MapController.UpdateAvailablePositions() offset loop for {gameObject} at position {position}, x is {x}, z is {z}, xMax is {xMax}, zMax is {zMax}");
            bool startingAlienAvailability = availableAlienPositions[x, z];
            availableBuildingPositions[x, z] = available;
            availableAlienPositions[x, z] = (availableBuildingPositions[x, z] && !alienExclusionArea[x, z]);

            if (availableAlienPositions[x, z] != startingAlienAvailability)
            {
                Vector3 pos = new Vector3(x, 0.25f, z);

                if (availableAlienPositions[x, z])
                {
                    alienSpawnablePositions.Add(pos);
                }
                else
                {
                    alienSpawnablePositions.Remove(pos);
                }
            }
        }
        else
        {
            Debug.Log($"{gameObject.name} can't update the availability of position {position}, which is outside the bounds of (0,0) to ({xMax},{zMax})");
        }
    }
}
