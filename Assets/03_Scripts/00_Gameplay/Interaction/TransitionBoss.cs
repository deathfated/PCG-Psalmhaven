using Psalmhaven;
using UnityEngine;

public class TransitionBoss : MonoBehaviour
{
    public Animator animator;
    PlayerController playerController;

    public void TriggerCutsceneBossFirst()
    {
        playerController.enabled = false;
        animator.SetBool("FirstBoss", true);
    }
}
