using UnityEngine;

namespace Psalmhaven
{
    public class CombatTrigger : MonoBehaviour
    {

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
        }
    }
}