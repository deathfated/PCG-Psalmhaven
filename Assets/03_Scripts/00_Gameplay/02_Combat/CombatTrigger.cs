using System;
using UnityEngine;
using UnityEngine.Events;

namespace Psalmhaven
{
    public class CombatTrigger : MonoBehaviour
    {
        public UnityEvent<GameObject> OnPlayerHit;


        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Player"))
            {
                OnPlayerCollide();
            }
        }

        private void OnPlayerCollide()
        {
            Debug.Log("Player collided with trigger!");
            CombatManager.Instance.StartCombat();
            OnPlayerHit?.Invoke(gameObject);
        }
    }
}