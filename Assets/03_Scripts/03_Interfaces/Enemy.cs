using System;
using System.Collections;
using UnityEngine;

namespace Psalmhaven
{
    public class Enemy : Character, IMask
    {

        //public float MaxDamage;
        [SerializeField] private string maskName;
        [SerializeField] private string[] combatActions;

        [SerializeField] private CombatTrigger trigger;

        public bool IsInCombat;

        public string MaskName => maskName;
        public string[] CombatActions => combatActions;

        public static event Action OnEnemyDied;


        public override void Die()
        {
            Debug.Log("Enemy ded");
            OnEnemyDied?.Invoke();

            Destroy(gameObject);
        }

        public void TurnOnHitbox()
        {
            StartCoroutine("ToggleHitbox");
        }

        private IEnumerator ToggleHitbox()
        {
            trigger.gameObject.SetActive(true);
            yield return new WaitForSeconds(1f);
            trigger.gameObject.SetActive(false);
        }
    }
}
