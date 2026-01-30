using UnityEngine;

namespace POPO.Dialogue
{
    public class DialogueTrigger : MonoBehaviour
    {
        [SerializeField] private Dialogue dialogueToTrigger;
        [SerializeField] private bool isTriggerOnStart = false;

        private void Start()
        {
            if (isTriggerOnStart && PopoDialogueManager.Instance != null) 
                TriggerDialogue();    
        }

        public void TriggerDialogue()
        {
            PopoDialogueManager.Instance.transform.GetChild(0).gameObject.SetActive(true);
            PopoDialogueManager.Instance.StartDialogue(dialogueToTrigger); 
        }
    }
}