using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public abstract class AttributeIcon : MonoBehaviour
    {
        [SerializeField] protected string initialAmount;
        [SerializeField] protected RectTransform amountIcon;
        [SerializeField] protected TextMeshProUGUI amountText;
        public void Start()
        {
            if (amountText != null) amountText.text = initialAmount;
        }
        public abstract void AnimateIcon();
        public virtual void SetAmount(float amount) 
        {
            AnimateIcon();
            amountText.text = amount.ToString("#0");
        }
    }
}