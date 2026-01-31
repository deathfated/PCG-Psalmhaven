using DG.Tweening;
using UnityEngine;

namespace UI
{
    public class GoldIcon : AttributeIcon
    {
        [Header("Animation")]
        [SerializeField] private Vector3 punchPower = new Vector3(1.2f, 1.2f, 1.2f);
        [SerializeField] private float punchDuration = 0.5f;
        public override void AnimateIcon()
        {
            amountIcon.DOKill();
            amountIcon.DOPunchScale(punchPower, punchDuration).SetEase(Ease.OutCubic).OnComplete(() => amountIcon.DORewind());
        }
    }
}