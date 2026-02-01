using Psalmhaven;
using UnityEngine;

public class PatrolEnemyContoller : BaseEnemyController
{
    [Header("Chasing Range")]
    [SerializeField] private float _chaseRange = 8f;
    [SerializeField] private float _maxChasingRange = 15f;

    public float currentDistance;

    public override void HasTriggerEnterEnemy()
    {
        base.HasTriggerEnterEnemy();

        ChangeState(EnemyState.Chasing);
    }

    public override void CheckTransitions()
    {
        if (!_hasTrigger)
            return;

        if (currentState == EnemyState.Interacting)
            return;

        currentDistance = Vector3.Distance(transform.position, playerTransform.position);

        if (currentDistance <= _chaseRange && currentState != EnemyState.Chasing)
        {
            ChangeState(EnemyState.Chasing);
        }

        if (currentState == EnemyState.Chasing)
        {
            if (currentDistance <= attackRange)
            {
                agent.isStopped = true;
                ChangeState(EnemyState.Interacting);
            }
            else if (currentDistance > _maxChasingRange)
            {
                ChangeState(EnemyState.Patrol);
            }
        }
    }
}
