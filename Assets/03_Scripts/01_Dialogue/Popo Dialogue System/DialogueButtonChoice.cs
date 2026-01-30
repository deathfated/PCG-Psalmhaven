using POPO.Dialogue;
using UnityEngine;

public class DialogueButtonChoice : MonoBehaviour
{
    public DChoice assignedChoice;

    public void TriggerChoice()
    {
        PopoDialogueManager.Instance.SelectChoice(assignedChoice);
    }
}
