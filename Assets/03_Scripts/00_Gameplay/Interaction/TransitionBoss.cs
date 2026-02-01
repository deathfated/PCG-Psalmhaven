using Psalmhaven;
using UnityEngine;

public class TransitionBoss : MonoBehaviour
{
    public Animator animator;
    public PlayerController playerController;
    public Transform enemyTransform;

    private void Start()
    {
        playerController = GameObject.FindWithTag("Player").GetComponent<PlayerController>();

    }

    public void TriggerCutsceneBossFirst()
    {
        playerController.transform.localPosition = Vector3.zero;
        enemyTransform.localPosition = Vector3.zero;
        playerController.enabled = false;
        animator.SetBool("FirstBoss", true);
    }
}
