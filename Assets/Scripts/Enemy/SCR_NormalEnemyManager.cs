using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class NormalEnemyManager : MonoBehaviour
{
    public enum NPCStates
    {
        Patrol,
        Chase,
        Attack,
        GroupAttack,
        StopChase
    }

    [SerializeField] private float chaseRange = 10f;
    [SerializeField] private float attackRange = 5f;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private Bullet Bullet;
    [SerializeField] private Slider healthBar;
    [SerializeField] private TextMeshProUGUI stateText;

    readonly float fireRate = 2f;
    public NPCStates currentState = NPCStates.Patrol;
    NavMeshAgent navMeshAgent;

    float nextShootTime = 0;
    private Vector3[] patrolPoints;
    private int nextPatrolPoint = 0;
    private Transform playerPosition;
    private float originalChaseRange;

    void Start()
    {
        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerPosition = playerObject.transform;
        }
        else
        {
            Debug.Log("Player object not found");
        }
        navMeshAgent = GetComponent<NavMeshAgent>();
        patrolPoints = GeneratePatrolPoints(transform.position, 3, 20.0f);

        gameObject.GetComponentInChildren<Canvas>().worldCamera = Camera.main;

    }

    void Update()
    {
        SwitchState();
    }

    private void SwitchState()
    {
        switch (currentState)
        {
            case NPCStates.Patrol:
                stateText.text = "Patrol";
                Patrol();
                break;
            case NPCStates.Chase:
                stateText.text = "Chase";
                Chase();
                break;
            case NPCStates.Attack:
                stateText.text = "Attack";
                Attack();
                break;
            case NPCStates.GroupAttack:
                stateText.text = "Group Attack";
                GroupAttack();
                break;
            case NPCStates.StopChase: // Add this case
                stateText.text = "Patrol";
                currentState = NPCStates.Patrol;
                break;
            default:
                stateText.text = "Patrol";
                Patrol();
                break;
        }
    }

    void OnEnable()
    {
        SuperEnemyManager.OnSuperEnemyAttack += StartChase;
        SuperEnemyManager.OnSuperEnemyStopAttack += StopChase;
    }

    void OnDisable()
    {
        SuperEnemyManager.OnSuperEnemyAttack -= StartChase;
        SuperEnemyManager.OnSuperEnemyStopAttack -= StopChase;
    }

    void StartChase()
    {
        originalChaseRange = chaseRange;
        chaseRange = 100f;
        currentState = NPCStates.GroupAttack;
    }

    void StopChase()
    {
        chaseRange = originalChaseRange;
        currentState = NPCStates.Patrol;
    }

    void GroupAttack()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, playerPosition.position);

        if (distanceToPlayer > chaseRange)
        {
            Patrol();
            return;
        }

        if (distanceToPlayer <= chaseRange && distanceToPlayer > attackRange)
        {
            Chase();
            return;
        }

        // Transition to Attack state if the player is within attack range
        if (distanceToPlayer <= attackRange)
        {
            Attack();
            return;
        }
    }

    private void Chase()
    {
        navMeshAgent.SetDestination(playerPosition.position);
        if (Vector3.Distance(transform.position, playerPosition.position) > chaseRange)
        {
            currentState = NPCStates.Patrol;
            return;
        }
        if (Vector3.Distance(transform.position, playerPosition.position) <= attackRange)
        {
            currentState = NPCStates.Attack;
            return;
        }
    }

    private void Attack()
    {
        navMeshAgent.isStopped = true;
        transform.LookAt(playerPosition);

        if (Time.time >= nextShootTime)
        {
            nextShootTime = Time.time + 1f / fireRate;
            Vector3 direction = (playerPosition.position - transform.position).normalized;
            Vector3 spawnPosition = new Vector3(bulletSpawnPoint.position.x, bulletSpawnPoint.position.y, bulletSpawnPoint.position.z);
            Bullet bullet = Instantiate(Bullet, spawnPosition, Quaternion.identity);
            bullet.tag = "Normal Bullet";
            bullet.GetComponent<Rigidbody>().velocity = direction * bullet.Speed;
        }

        if (Vector3.Distance(transform.position, playerPosition.position) > attackRange)
        {
            navMeshAgent.isStopped = false;
            currentState = NPCStates.Chase;
            return;
        }
    }

    public Vector3[] GeneratePatrolPoints(Vector3 startPosition, int numPoints, float radius)
    {
        Vector3[] patrolPoints = new Vector3[numPoints];
        for (int i = 0; i < numPoints; i++)
        {
            Vector3 randomDirection = UnityEngine.Random.insideUnitSphere * radius;
            randomDirection += startPosition;
            NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, 1);
            patrolPoints[i] = hit.position;
        }
        return patrolPoints;
    }

    private void Patrol()
    {
        if (navMeshAgent.remainingDistance <= navMeshAgent.stoppingDistance)
        {
            nextPatrolPoint = (nextPatrolPoint + 1) % patrolPoints.Length;
            navMeshAgent.SetDestination(patrolPoints[nextPatrolPoint]);
        }

        if (Vector3.Distance(transform.position, playerPosition.position) <= chaseRange)
        {
            currentState = NPCStates.Chase;
            return;
        }
    }

    public void TakeDamage(float damage)
    {
        healthBar.value -= damage;
        if (healthBar.value <= 0f)
        {
            PlayerManager playeManager = GameObject.FindGameObjectWithTag("Player").GetComponentInParent<PlayerManager>();
            playeManager.AddPoints(10);
            Destroy(gameObject);
        }
    }
}