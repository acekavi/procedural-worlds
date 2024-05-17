using System;
using System.Collections;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float Speed = 10f;
    private readonly float LifeTime = 5f;

    private void Start()
    {
        StartCoroutine(DestroyAfterTime());
    }

    private void OnTriggerEnter(Collider collidedObject)
    {
        if (collidedObject.CompareTag("Wall") || collidedObject.CompareTag("Enemy") || collidedObject.CompareTag("Player"))
        {
            Destroy(gameObject);
        }
        if (collidedObject.CompareTag("Player"))
        {
            if (gameObject.CompareTag("Normal Bullet"))
            {
                collidedObject.gameObject.GetComponentInParent<PlayerManager>().TakeDamage(5f);
            }
            else if (gameObject.CompareTag("Super Bullet"))
            {
                collidedObject.gameObject.GetComponentInParent<PlayerManager>().TakeDamage(10f);
            }
        }
        if (collidedObject.CompareTag("Enemy"))
        {
            if (gameObject.CompareTag("Normal Bullet") || gameObject.CompareTag("Super Bullet"))
            {
                return;
            }
            if (collidedObject.gameObject.GetComponentInParent<NormalEnemyManager>() != null)
            {
                collidedObject.gameObject.GetComponentInParent<NormalEnemyManager>().TakeDamage(20f);
            }
            else if (collidedObject.gameObject.GetComponentInParent<SuperEnemyManager>() != null)
            {
                collidedObject.gameObject.GetComponentInParent<SuperEnemyManager>().TakeDamage(10f);
            }

        }
    }

    IEnumerator DestroyAfterTime()
    {
        yield return new WaitForSeconds(LifeTime);
        Destroy(gameObject);
    }
}