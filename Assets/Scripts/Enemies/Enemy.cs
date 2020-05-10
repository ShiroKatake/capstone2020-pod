﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Demo class for enemies.
/// </summary>
public class Enemy : MonoBehaviour
{
    //Private Fields---------------------------------------------------------------------------------------------------------------------------------

    //Serialized Fields----------------------------------------------------------------------------

    [Header("Enemy Stats")] 
    [SerializeField] private int id;
    [SerializeField] private float speed;
    [SerializeField] private float turningSpeed;
    [SerializeField] private float damage;
    [SerializeField] private float attackCooldown;

    //Non-Serialized Fields------------------------------------------------------------------------
    [Header("Testing")]
    //Componenets
    private Health health;
    private Rigidbody rigidbody;

    //Movement
    private bool moving;
    private float groundHeight;

    //Turning
    private Quaternion oldRotation;
    private Quaternion targetRotation;
    private float slerpProgress;
    
    //Targeting
    private Building cryoEgg;
    private List<Transform> visibleAliens;
    private List<Transform> visibleTargets;
    [SerializeField] private Transform target;
    [SerializeField] private Health targetHealth;
    [SerializeField] private Transform shotBy;
    private float timeOfLastAttack;

    //Public Properties------------------------------------------------------------------------------------------------------------------------------

    //Basic Public Properties----------------------------------------------------------------------

    /// <summary>
    /// Enemy's Health component.
    /// </summary>
    public Health Health { get => health; }

    /// <summary>
    /// Whether or not the Enemy is moving.
    /// </summary>
    public bool Moving { get => moving; set => moving = value; }

    /// <summary>
    /// The player or building the enemy was shot by most recently.
    /// </summary>
    public Transform ShotBy { get => shotBy; set => shotBy = value; }

    //Complex Public Properties--------------------------------------------------------------------

    /// <summary>
    /// Enemy's unique ID number. Id should only be set by Enemy.Setup().
    /// </summary>
    public int Id
    {
        get
        {
            return id;
        }

        set
        {
            id = value;
            gameObject.name = $"Enemy {id}";
        }
    }

    //Initialization Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Awake() is run when the script instance is being loaded, regardless of whether or not the script is enabled. 
    /// Awake() runs before Start().
    /// </summary>
    void Awake()
    {
        health = GetComponent<Health>();
        rigidbody = GetComponent<Rigidbody>();

        cryoEgg = BuildingController.Instance.CryoEgg;

        groundHeight = transform.position.y;

        visibleAliens = new List<Transform>();
        visibleTargets = new List<Transform>();
    }

    /// <summary>
    /// Prepares the Enemy to chase its targets when EnemyFactory puts it in the world. 
    /// </summary>
    public void Setup(int id)
    {
        Id = id;
        health.Reset();
        target = cryoEgg.GetComponentInChildren<Collider>().transform;
        targetHealth = cryoEgg.Health;
        timeOfLastAttack = attackCooldown * -1;

        //Rotate to face the cryo egg
        Vector3 targetRotation = cryoEgg.transform.position - transform.position;
        transform.rotation = Quaternion.LookRotation(targetRotation);
    }

    //Core Recurring Methods-------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// Update() is run every frame.
    /// </summary>
    //private void Update()
    //{
        
    //}

    /// <summary>
    /// FixedUpdate() is run at a fixed interval independant of framerate.
    /// </summary>
    private void FixedUpdate()
    {
        CheckHealth();
        SelectTarget();
        Look();
        Move();
    }

    //Recurring Methods (FixedUpdate())-------------------------------------------------------------------------------------------------------------  

    /// <summary>
    /// Checks if Enemy has 0 health, destroying it if it has.
    /// </summary>
    private void CheckHealth()
    {
        if (health.IsDead())
        {
            EnemyFactory.Instance.DestroyEnemy(this);
        }
    }

    /// <summary>
    /// Selects the most appropriate target for the enemy.
    /// </summary>
    private void SelectTarget()
    {
        if (visibleTargets.Count > 0)
        {
            if (shotBy != null && visibleTargets.Contains(shotBy))
            {
                target = shotBy;
                targetHealth = target.GetComponentInParent<Health>();   //Gets Health from target or any of its parents that has it.
                return;
            }

            float distance = 99999999999;
            float closestDistance = 9999999999999999;
            Transform closestTarget = null;

            foreach (Transform t in visibleTargets)
            {
                distance = Vector3.Distance(transform.position, t.position);

                if (closestTarget == null || distance < closestDistance)
                {
                    closestTarget = t;
                    closestDistance = distance;
                }
            }

            if (target != closestTarget)
            {
                target = closestTarget;
                targetHealth = target.GetComponentInParent<Health>();   //Gets Health from target or any of its parents that has it.
            }
        }
        else if (target != cryoEgg.transform)
        {
            target = cryoEgg.GetComponentInChildren<Collider>().transform;
            targetHealth = cryoEgg.Health;
        }
    }

    /// <summary>
    /// Alien uses input information to determine which direction it should be facing
    /// </summary>
    private void Look()
    {
        //TODO: swarm-based looking behaviour
        Vector3 newRotation = target.position - transform.position;

        if (newRotation != targetRotation.eulerAngles)
        {
            oldRotation = transform.rotation;
            targetRotation = Quaternion.LookRotation(newRotation);
            slerpProgress = 0f;
        }

        if (slerpProgress < 1)
        {
            slerpProgress = Mathf.Min(1, slerpProgress + turningSpeed * Time.fixedDeltaTime);
            transform.rotation = Quaternion.Slerp(oldRotation, targetRotation, slerpProgress);
        }
    }

    /// <summary>
    /// Moves Enemy.
    /// </summary>
    private void Move()
    {
        transform.Translate(new Vector3(0, 0, speed * Time.fixedDeltaTime));

        //Toggle gravity if something has pushed the enemy up above groundHeight
        if (rigidbody.useGravity)
        {
            if (transform.position.y <= groundHeight)
            {
                transform.position = new Vector3(transform.position.x, groundHeight, transform.position.z);
                rigidbody.useGravity = false;
            }
        }
        else
        {
            if (transform.position.y > groundHeight)   //TODO: account for terrain pushing the enemy up, if it can move up hills?
            {
                rigidbody.useGravity = true;
            }
        }
    }

    //Triggered Methods------------------------------------------------------------------------------------------------------------------------------

    /// <summary>
    /// OnCollisionStay is called once per frame for every collider/rigidbody that is touching rigidbody/collider.
    /// </summary>
    /// <param name="collision">The collision data associated with this event.</param>
    private void OnCollisionStay(Collision collision)
    {
        if (!collision.collider.isTrigger && (collision.collider.CompareTag("Building") || collision.collider.CompareTag("Player")))
        {
            if (Time.time - timeOfLastAttack > attackCooldown)
            {
                timeOfLastAttack = Time.time;
                targetHealth.Value -= damage;
            }
        }
        //TODO: if made contact with target and target is a building, step back a smidge and attack, so that OnCollisionStay is not called every single frame. For player, check if within attack range to verify that the enemy can still attack them?
    }

    /// <summary>
    /// When a GameObject collides with another GameObject, Unity calls OnTriggerEnter.
    /// </summary>
    /// <param name="other">The other Collider involved in this collision.</param>
    private void OnTriggerEnter(Collider collider)
    {
        if (!collider.isTrigger)
        {
            if (collider.CompareTag("Enemy"))
            {
                visibleAliens.Add(collider.transform);
            }
            else if (collider.CompareTag("Building"))
            {
                visibleTargets.Add(collider.transform.parent);
            }
            else if (collider.CompareTag("Player"))
            {
                visibleTargets.Add(collider.transform);
            }
            else if (collider.CompareTag("Projectile"))
            {
                Debug.Log("Enemy.OnTriggerEnter; Enemy hit by a projectile");
                Projectile projectile = collider.GetComponent<Projectile>();
                shotBy = projectile.Owner.GetComponentInChildren<Collider>().transform;
            }
        }
    }

    /// <summary>
    /// OnTriggerExit is called when the Collider other has stopped touching the trigger.
    /// </summary>
    /// <param name="other">The other Collider involved in this collision.</param>
    private void OnTriggerExit(Collider collider)
    {
        if (!collider.isTrigger)
        {
            if (visibleAliens.Contains(collider.transform))
            {
                visibleAliens.Remove(collider.transform);
                return;
            }

            if (visibleTargets.Contains(collider.transform))
            {
                visibleTargets.Remove(collider.transform);
                //return;
            }
        }
    }
}
