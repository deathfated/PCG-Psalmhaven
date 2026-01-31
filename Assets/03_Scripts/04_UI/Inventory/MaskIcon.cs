namespace UI
{
    public class MaskIcon : AttributeIcon
    {
        int maskAmount = 0;
        public override void AnimateIcon()
        {
            
        }
        public override void SetAmount(float amount)
        {
            maskAmount += (int) amount;
        }
    }
}