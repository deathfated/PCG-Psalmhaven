using DG.Tweening;
using UnityEngine;

namespace UI 
{
    public class HealthIcon : AttributeIcon
    {
        [Header("Animation")]
        [SerializeField] private float shakeDuration = 0.5f;
        [SerializeField] private float shakeStrength = 1;
        [SerializeField] private int vibration = 10;
        [Header("Color")]
        [SerializeField] private Color startingColor;
        [SerializeField] private Color damagedColor;

        public override void AnimateIcon()
        {
            //Sequence sequence = DOTween.Sequence();
            //sequence.AppendCallback(()=> )
            amountIcon.DOKill();
            amountIcon.DOShakeAnchorPos(shakeDuration, shakeStrength, vibration).OnComplete(()=> amountIcon.DORewind());
        }
    }
}
