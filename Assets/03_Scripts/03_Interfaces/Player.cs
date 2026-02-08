using System;
using Psalmhaven.UI;
using UnityEngine;

namespace Psalmhaven
{
    public class Player : Character
    {
        public int currentMask;
        public string[] combatEffectData;
        public string[] combatActions;
        public string[] combatActions2;
        public int[] runActions;

        public static event Action OnPlayerDied;
          
        public override void Die()
        {
            Debug.Log("Anjir mati");
            OnPlayerDied?.Invoke();

        }

        public override void TakeDamage(float damage)
        {
            base.TakeDamage(damage);
            UIManager.instance.SetHealth(currentHealth);

        }

        public override void Heal(float amount)
        {
            base.Heal(amount);
            UIManager.instance.SetHealth(currentHealth);
        }
    }
}