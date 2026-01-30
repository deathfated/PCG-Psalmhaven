using UnityEngine;

namespace POPO.Dialogue
{
    [CreateAssetMenu(fileName = "DChoice", menuName = "Popo/DChoice")]
    public class DChoice : ScriptableObject
    {
        public string choiceText;
        public Dialogue nextDialogue;
    }
}
