﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Controller class for aliens.
/// </summary>
public class AlienController : MonoBehaviour
{
    //Private Fields---------------------------------------------------------------------------------------------------------------------------------

    //Serialized Fields----------------------------------------------------------------------------

    [Header("Spawning Stats")]
    [SerializeField] private float respawnDelay;

    [Header("Swarm Stats")]
    [SerializeField] private Vector3 tutorialSwarmCentre;
    [SerializeField] private int maxSwarmRadius;
    [SerializeField] private int maxSwarmSize;
    [SerializeField] private int maxSwarmCount;

    [Header("Penalty Stats")]
    [SerializeField] private float defencePenaltyThreshold;
    [SerializeField] private float nonDefencePenaltyThreshold;
    [SerializeField] private int penaltyIncrement;
    [SerializeField] private float penaltyCooldown;

    [Header("For Testing")]
    [SerializeField] private bool spawnAliens;
    [SerializeField] private bool spawnAlienNow;
    //[SerializeField] private Vector3 testSpawnPos;
    [SerializeField] private bool ignoreDayNightCycle;

    //Non-Serialized Fields------------------------------------------------------------------------

    //Alien Spawning
    private List<Alien> aliens;
    private float timeOfLastDeath;
    private Dictionary<int, List<Vector3>> swarmOffsets;
    private LayerMask groundLayerMask;
    private List<EStage> spawnableStages;

    //Penalty Incrementation
    private int spawnCountPenalty;
    private float timeOfLastPenalty;

    //PublicProperties-------------------------------------------------------------------------------------------------------------------------------

    //Singleton Public Property--------------------------------------------------------------------

    /// <summary>
    /// AlienController's singleton public property.
    /// </summary>
    public static AlienController Instance { get; protected set; }

    //Basic Public Properties----------------------------------------------------------------------

    /// <summary>
    /// A list of all aliens
    /// </summary>
    public List<Alien> Aliens { get => aliens; }

    //Complex Public Properties--------------------------------------------------------------------

    //Initialization Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Awake() is run when the script instance is being loaded, regardless of whether or not the script is enabled. 
    /// Awake() runs before Start().
    /// </summary>
    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("There should never be 2 or more AlienControllers.");
        }

        Instance = this;
        aliens = new List<Alien>();
        timeOfLastDeath = respawnDelay * -1;
        timeOfLastPenalty = penaltyCooldown * -1;
        spawnCountPenalty = 0;
        groundLayerMask = LayerMask.GetMask("Ground");
        spawnableStages = new List<EStage>() { EStage.Combat, EStage.MainGame };

        //Setting up position offsets that can be randomly selected from for cluster spawning 
        swarmOffsets = new Dictionary<int, List<Vector3>>();

        for (int i = 0; i <= maxSwarmRadius; i++)
        {
            swarmOffsets[i] = new List<Vector3>();
        }

        for (int i = maxSwarmRadius * -1; i <= maxSwarmRadius; i++)
        {
            for (int j = maxSwarmRadius * -1; j <= maxSwarmRadius; j++)
            {
                int iMag = MathUtility.Instance.IntMagnitude(i);
                int jMag = MathUtility.Instance.IntMagnitude(j);
                Vector3 pos = new Vector3(i, 0, j);

                foreach (KeyValuePair<int, List<Vector3>> p in swarmOffsets)
                {
                    if ((iMag == p.Key || jMag == p.Key) && iMag <= p.Key && jMag <= p.Key)
                    {
                        p.Value.Add(pos);
                    }
                }
            }
        }
    }

    //Core Recurring Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Update() is run every frame.
    /// </summary>
    private void Update()
    {
        SpawnAliens();
    }

    //Recurring Methods (Update())-------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Spawns more aliens on a regular basis.
    /// </summary>
    private void SpawnAliens()
    {
        if (spawnAliens && spawnableStages.Contains(StageManager.Instance.CurrentStage.ID) && (spawnAlienNow || (!ClockController.Instance.Daytime && aliens.Count == 0 && Time.time - timeOfLastDeath > respawnDelay)))
        {
            //Start Testing----------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            //Debug.Log("Nighttime? No aliens? Spawning time!");
            if (spawnAlienNow)
            {
                //Debug.Log("Test position start");
                spawnAlienNow = false;

                //Vector3 pos = MapController.Instance.RandomAlienSpawnablePos(new List<Vector3>());
                //List<Vector3> testPositions = new List<Vector3>()
                //{
                //    new Vector3(30, pos.y, 30), //Test position on nav mesh
                //    new Vector3(20, pos.y, 155), //Test position on nav mesh
                //    new Vector3(20, pos.y, 130), //Test position on nav mesh
                //    new Vector3(20, pos.y, 100), //Test position on nav mesh
                //    new Vector3(20, pos.y, 70), //Test position on nav mesh
                //    new Vector3(25, pos.y, 40), //Test position on nav mesh
                //    new Vector3(130, pos.y, 180), //Test position on nav mesh
                //    new Vector3(191, pos.y, 133), //Test position on nav mesh
                //    new Vector3(300, pos.y, 300), //Test position on nav mesh
                //    new Vector3(2, pos.y, 33),   //Test position off nav mesh on plane
                //    new Vector3(43, pos.y, 54),   //Test position off nav mesh on plateau
                //    new Vector3(43, pos.y, 80),   //Test position off nav mesh on plateau
                //    new Vector3(160, pos.y, 25),   //Test position off nav mesh on plateau
                //    new Vector3(29, pos.y, 195),   //Test position off nav mesh on plateau
                //    //new Vector3(58, pos.y, 39),   //Test position off nav mesh in pit     //Excluded anyway by other mechanisms
                //};

                //foreach (Vector3 testPos in testPositions)
                //{
                //    Debug.Log($"Testing position {testPos}");

                //    if (MapController.Instance.PositionAvailableForSpawning(testPos, true))
                //    {
                //        RaycastHit rayHit;
                //        NavMeshHit navHit;
                //        NavMeshPath path = new NavMeshPath();
                //        Physics.Raycast(testPos, Vector3.down, out rayHit, 25, groundLayerMask);
                //        Vector3 heightAdjustedPos = new Vector3(testPos.x, rayHit.point.y, testPos.z);
                //        Alien alien = AlienFactory.Instance.GetAlien(heightAdjustedPos);
                //        alien.Setup(IdGenerator.Instance.GetNextId());

                //        //if (alien.NavMeshAgent.isOnNavMesh)           //All true
                //        //if (alien.NavMeshAgent.hasPath)               //All false
                //        //if (alien.NavMeshAgent.isOnOffMeshLink)       //All false

                //        //alien.ActivateStationaryNavMeshAgent();                                           //Breaks
                //        //if (alien.NavMeshAgent.CalculatePath(CryoEgg.Instance.transform.position, path))                  
                //        //{
                //        //    while (!alien.NavMeshAgent.hasPath)
                //        //    {
                //        //        Debug.Log($"Path pending . . . alien destination is {alien.NavMeshAgent.destination}");
                //        //        yield return null;
                //        //    }
                //        //     
                //        //    if (alien.NavMeshAgent.path.status != NavMeshPathStatus.PathComplete)

                //        if (NavMesh.SamplePosition(alien.transform.position, out navHit, 1, NavMesh.AllAreas))
                //        { 
                //            aliens.Add(alien);
                //            Debug.Log($"Successful spawn at pos {alien.transform.position}");
                //        }
                //        else
                //        {
                //            Debug.LogError($"NavMesh.SamplePosition returned false at {alien.transform.position}, therefore found a position not on the nav mesh");
                //            MapController.Instance.RegisterOffMeshPosition(testPos);
                //            AlienFactory.Instance.DestroyAlien(alien);
                //        }
                //    }
                //    else
                //    {
                //        Debug.LogError($"Could not spawn at {testPos} as position is unavailable for spawning, regardless of if it's on the nav mesh or not.");
                //    }
                //}

                //Debug.Log("Test position end");
            }

            //End Testing------------------------------------------------------------------------------------------------------------------------------------------------------------------------------

            //Check and increment penalty
            if (Time.time - timeOfLastPenalty > penaltyCooldown && (Time.time - BuildingController.Instance.TimeLastDefenceWasBuilt > defencePenaltyThreshold || Time.time - BuildingController.Instance.TimeLastNonDefenceWasBuilt > nonDefencePenaltyThreshold))
            {
                spawnCountPenalty += penaltyIncrement;
                timeOfLastPenalty = Time.time;
                //Debug.Log($"AlienController.spawnCountPenalty incremented to {spawnCountPenalty}");
            }

            //Spawn aliens
            EStage currentStage = StageManager.Instance.CurrentStage.ID;
            int spawnCount = (currentStage == EStage.Combat ? 3 : BuildingController.Instance.BuildingCount * 3 + spawnCountPenalty);
            //Debug.Log($"Current stage: {currentStage}, Spawn count: {spawnCount}");
            Vector3 swarmCentre = Vector3.zero; 
            int swarmSize = 0;                   
            int swarmRadius = 0;
            int swarmCount = 0;
            float offsetMultiplier = 2;
            List<Vector3> availableOffsets = new List<Vector3>();
            Dictionary<Vector3, bool> unavailablePositions = new Dictionary<Vector3, bool>();

            for (int i = 0; i < spawnCount; i++)
            //for (int i = 0; i < 100; i++)
            {
                if (availableOffsets.Count == 0)
                {
                    if (swarmRadius >= swarmOffsets.Count || swarmSize >= maxSwarmSize)
                    {
                        swarmCount++;

                        if (swarmCount >= maxSwarmCount)
                        {
                            return;
                        }

                        swarmCentre = MapController.Instance.RandomAlienSpawnablePos(new List<Vector3>(unavailablePositions.Keys));     //RandomAlienSpawnablePos() checks the stage before selecting its list of normally available positions
                        //Debug.Log($"Swarm centre: {swarmCentre}");
                        swarmRadius = 0;
                        swarmSize = 0;
                    }

                    availableOffsets.AddRange(swarmOffsets[swarmRadius]);
                    swarmRadius++;
                }

                int j = Random.Range(0, availableOffsets.Count);
                Vector3 spawnPos = swarmCentre + availableOffsets[j] * offsetMultiplier;
                availableOffsets.RemoveAt(j);
                //Debug.Log($"spawnPos: {spawnPos}");

                if (MapController.Instance.PositionAvailableForSpawning(spawnPos, true) || currentStage == EStage.Combat)
                {
                    RaycastHit rayHit;
                    NavMeshHit navHit;
                    Physics.Raycast(spawnPos, Vector3.down, out rayHit, 25, groundLayerMask);
                    Alien alien = AlienFactory.Instance.GetAlien(new Vector3(spawnPos.x, rayHit.point.y, spawnPos.z));
                    alien.Setup(IdGenerator.Instance.GetNextId());
                    //Debug.Log($"Spawned and set up {alien} at {alien.transform.position}");

                    if (NavMesh.SamplePosition(alien.transform.position, out navHit, 1, NavMesh.AllAreas))
                    {
                        //Debug.Log($"Successfully spawned {alien} at pos {alien.transform.position}");
                        aliens.Add(alien);
                        swarmSize++;
                    }
                    else
                    {
                        //Debug.LogError($"NavMesh.SamplePosition returned false at {alien.transform.position}, therefore found a position not on the nav mesh");
                        MapController.Instance.RegisterOffMeshPosition(spawnPos);
                        AlienFactory.Instance.DestroyAlien(alien);
                        i--;
                    }

                    int maxLeft = (int)(maxSwarmRadius * offsetMultiplier * -1);
                    int maxRight = Mathf.CeilToInt(maxSwarmRadius * offsetMultiplier);

                    for (int m = maxLeft; m <= maxRight; m++)
                    {
                        for (int n = maxLeft; n <= maxRight; n++)
                        {
                            Vector3 q = new Vector3(spawnPos.x + m, spawnPos.y, spawnPos.z + n);
                            unavailablePositions[q] = true;
                        }
                    }                          
                }
                else
                {
                    i--;
                }                        
            }
        }
    }

    /// <summary>
    /// Removes the alien from AlienController's list of aliens.
    /// </summary>
    /// <param name="alien">The alien to be removed from AlienController's list of aliens.</param>
    public void DeRegisterAlien(Alien alien)
    {
        if (aliens.Contains(alien))
        {
            aliens.Remove(alien);
            timeOfLastDeath = Time.time;
        }
    }
}
