using UnityEngine;

namespace POPO.Dialogue
{

    public class DButton : MonoBehaviour
    {
        public DChoice selectedChoice;

        public void ContinueDialogue()
        {
            if (selectedChoice != null)
            {
                PopoDialogueManager.Instance.SelectChoice(selectedChoice);
                selectedChoice = null;
            }
        }
    }
}