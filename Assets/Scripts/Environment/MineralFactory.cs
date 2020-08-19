﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A factory class for minerals.
/// </summary>
public class MineralFactory : Factory<MineralFactory, Mineral, ENone>
{
    //Private Fields---------------------------------------------------------------------------------------------------------------------------------  

    //Serialized Fields----------------------------------------------------------------------------                                                    

    [Header("Mineral Stats")]
	[SerializeField] private int oreCount;
    [SerializeField] private bool destroySpentMinerals;

    //Non-Serialized Fields------------------------------------------------------------------------

    private List<Mineral> despawningMinerals;
    private List<Mineral> despawnedMinerals;

    //Public Properties------------------------------------------------------------------------------------------------------------------------------

    //Singleton Public Property--------------------------------------------------------------------                                                    

    /// <summary>
    /// How much ore should a single mineral node yield in total.
    /// </summary>
	public int OreCount { get => oreCount;}

	//Initialization Methods-------------------------------------------------------------------------------------------------------------------------

	/// <summary>
	/// Awake() is run when the script instance is being loaded, regardless of whether or not the script is enabled. 
	/// Awake() runs before Start().
	/// </summary>
	protected override void Awake()
    {
        base.Awake();
        despawningMinerals = new List<Mineral>();
        despawnedMinerals = new List<Mineral>();        
    }

    /// <summary>
    /// Start() is run on the frame when a script is enabled just before any of the Update methods are called for the first time. 
    /// Start() runs after Awake().
    /// </summary>
    protected override void Start()
    {
        base.Start();

        foreach (Mineral m in pool[ENone.None])
        {
            m.DisableColliders();
        }
    }

    //Triggered Methods -----------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Get a new mineral node.
    /// </summary>
    /// <param name="position">The position the mineral should be instantiated at.</param>
    /// <returns>A mineral node.</returns>
    public Mineral Get(Vector3 position)
    {
        return Get(ENone.None, position);
    }

    /// <summary>
    /// Get a new mineral node.
    /// </summary>
    /// <param name="type">The type of mineral to retrieve.</param>
    /// <param name="position">The position the mineral should be instantiated at.</param>
    /// <returns>A mineral node.</returns>
    public override Mineral Get(ENone type, Vector3 position)
    {
        Mineral mineral = base.Get(type, position);
        mineral.Id = IdGenerator.Instance.GetNextId();
        MapController.Instance.RegisterMineral(mineral);
        return mineral;
    }

    /// <summary>
    /// Custom modifications to a mineral after Get() retrieves it from the pool.
    /// </summary>
    /// <param name="mineral">The mineral being modified.</param>
    /// <returns>The modified mineral.</returns>
    protected override Mineral GetRetrievalSetup(Mineral mineral)
    {
        mineral.EnableColliders();
        return mineral;
    }

    /// <summary>
    /// Destroy a mineral node.
    /// </summary>
    /// <param name="mineral">The mineral to destroy.</param>
    public void Destroy(Mineral mineral)
    {
        Destroy(ENone.None, mineral);
    }

    /// <summary>
    /// Destroy a mineral node.
    /// </summary>
    /// <param name="mineral">The mineral to destroy.</param>
    public override void Destroy(ENone type, Mineral mineral)
    {
        MapController.Instance.DeRegisterMineral(mineral);
        mineral.Reset();
        despawningMinerals.Add(mineral);

        if (despawningMinerals.Count == 1)
        {
            StartCoroutine(PoolDespawningMinerals());
        }
    }

    /// <summary>
    /// Transfers despawned minerals to the minerals pool when they're finished despawning.
    /// </summary>
    private IEnumerator PoolDespawningMinerals()
    {
        while (despawningMinerals.Count > 0)
        {
            for (int i = 0; i < despawningMinerals.Count; i++)
            {
                if (!despawningMinerals[i].Despawning)
                {
                    despawnedMinerals.Add(despawningMinerals[i]);
                    despawningMinerals.RemoveAt(i);
                    i--;
                }
            }

            if (despawnedMinerals.Count > 0)
            {
                do
                {
                    Mineral toDestroy = despawnedMinerals[0];
                    despawnedMinerals.RemoveAt(0);

                    if (destroySpentMinerals)
                    {
                        GameObject.Destroy(toDestroy.gameObject);
                    }
                    else
                    {
                        base.Destroy(ENone.None, toDestroy);
                    }
                }
                while (!destroySpentMinerals && despawnedMinerals.Count > 0);
            }

            yield return null;
        }        
    }
}
