using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollectibleManager : MonoBehaviour
{
    void OnTriggerEnter(Collider collidedObject)
    {
        if (collidedObject.CompareTag("Player"))
        {
            PlayerManager playeManager = collidedObject.GetComponentInParent<PlayerManager>();

            if (gameObject.CompareTag("Trap"))
            {
                playeManager.TakeDamage(20.0f);

                // Destroy the current object
                Destroy(gameObject);
            }
            else if (gameObject.CompareTag("Medikit"))
            {
                playeManager.Heal(10.0f);

                // Destroy the current object
                Destroy(gameObject);
            }
            else if (gameObject.CompareTag("Cake"))
            {
                playeManager.DoubleJumpForce();
                playeManager.ShowFeedback("Double jump force activated!", 5);
                // Destroy the current object
                Destroy(gameObject);
            }
            else if (gameObject.CompareTag("Ultimate"))
            {

                playeManager.DoubleSpeed();
                playeManager.ShowFeedback("Double speed force activated!", 10);
                // Destroy the current object
                Destroy(gameObject);
            }
            else if (gameObject.CompareTag("Coins"))
            {
                playeManager.AddPoints(10);
                // Destroy the current object
                Destroy(gameObject);
            }
            else if (gameObject.CompareTag("RedPill"))
            {
                playeManager.SlowMotion();
                playeManager.ShowFeedback("Player slow motion activated!", 5);
                // Destroy the current object
                Destroy(gameObject);
            }
        }
    }
}
