using Psalmhaven;
using UnityEngine;

public class BossFightCutscene : BaseEnemyController
{
    private float _currentDistance;
    private bool _hastrigger;

    [SerializeField] CombatTrigger trigger;

    public void OnHitCombatHitbox(GameObject trigger)
    {
        ChangeState(EnemyState.Idle);
        _animator.CrossFade("Anim_Idle", 0.2f);
    }

    public override void CheckTransitions()
    {
        _currentDistance = Vector3.Distance(transform.position, playerTransform.position);

        if (currentState == EnemyState.Chasing)
        {
            if (_currentDistance <= attackRange)
            {
                _animator.CrossFade("Anim_Idle_Fly_Sword_Attack", 0.2f);
                agent.isStopped = true;
                ChangeState(EnemyState.Interacting);
            }
        }
    }

    public override void HasTriggerEnterEnemy()
    {
        if (_hastrigger)
            return;

        base.HasTriggerEnterEnemy();

        ChangeState(EnemyState.Chasing);
        _animator.SetBool("IsRunning", true);
        _hastrigger = true;

        PlayerController player = playerTransform.GetComponent<PlayerController>();
        if (player != null)
        {
            //player.DisableMovement();
        }
    }


}
