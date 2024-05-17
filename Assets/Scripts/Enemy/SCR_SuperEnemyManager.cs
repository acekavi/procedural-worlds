using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class SuperEnemyManager : MonoBehaviour
{
    public enum NPCStates
    {
        Patrol,
        Chase,
        Attack
    }

    [SerializeField] private float chaseRange = 15f;
    [SerializeField] private float attackRange = 10f;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private Bullet Bullet;
    [SerializeField] private Slider healthBar;
    [SerializeField] private TextMeshProUGUI stateText;

    readonly float fireRate = 2f;
    NPCStates currentState = NPCStates.Patrol;
    NavMeshAgent navMeshAgent;

    float nextShootTime = 0;
    private Vector3[] patrolPoints;
    private int nextPatrolPoint = 0;
    private Transform playerPosition;
    private readonly float navRadius = 20f;

    public static Action OnSuperEnemyAttack; // Event to be invoked when SuperEnemy starts attacking
    public static Action OnSuperEnemyStopAttack; // Event to be invoked when SuperEnemy stops attacking

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
        patrolPoints = GeneratePatrolPoints(transform.position, 3, navRadius);

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
            default:
                stateText.text = "Patrol";
                Patrol();
                break;
        }
    }

    void StartAttack()
    {

        OnSuperEnemyAttack?.Invoke(); // Invoke the event when SuperEnemy starts attacking
    }

    void StopAttack()
    {

        OnSuperEnemyStopAttack?.Invoke(); // Invoke the event when SuperEnemy stops attacking
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

        StartAttack(); // Invoke the event when SuperEnemy attacks

        if (Time.time >= nextShootTime)
        {
            nextShootTime = Time.time + 1f / fireRate;
            Vector3 direction = (playerPosition.position - transform.position).normalized;
            Vector3 spawnPosition = new Vector3(bulletSpawnPoint.position.x, bulletSpawnPoint.position.y, bulletSpawnPoint.position.z);
            Bullet bullet = Instantiate(Bullet, spawnPosition, Quaternion.identity);
            bullet.tag = "Super Bullet";
            bullet.GetComponent<Rigidbody>().velocity = direction * bullet.Speed;
        }

        if (Vector3.Distance(transform.position, playerPosition.position) > attackRange)
        {
            StopAttack(); // Invoke the event when SuperEnemy stops attacking
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
            PlayerManager playerManager = GameObject.FindGameObjectWithTag("Player").GetComponentInParent<PlayerManager>();
            playerManager.AddPoints(20);
            StopAttack(); // Invoke the event when SuperEnemy stops attacking
            Destroy(gameObject);
        }
    }
}
