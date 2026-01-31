using UnityEngine;

namespace UI
{
    public class Inventory : MonoBehaviour
    {
        [SerializeField] private HealthIcon healthIcon;
        [SerializeField] private MaskIcon maskIcon;
        [SerializeField] private GoldIcon goldIcon;
        public void SetHealth(float health)
        {
            healthIcon.SetAmount(health);
        }
        public void SetGold(float gold)
        {
            goldIcon.SetAmount(gold);
        }
    }
}
