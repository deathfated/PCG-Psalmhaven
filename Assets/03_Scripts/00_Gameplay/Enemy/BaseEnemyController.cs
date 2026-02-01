using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;

public abstract class BaseEnemyController : MonoBehaviour
{
    public EnemyState currentState;
    public EnemyState defaultState;
    public float attackRange = 0.5f;
    public float rotationSpeed = 0.5f;
    public float stopDistanceAttack = 2f;

    [Header("Component Dependencies")]
    [SerializeField] protected Animator _animator;
    [SerializeField] private EnemyTrigger _enemyTrigger;

    protected Transform playerTransform;
    protected NavMeshAgent agent;

    [SerializeField] protected Transform[] _patrolPoints;
    [SerializeField] protected float _idleTime;
    private bool _patrolIdle;
    private float _currentIdleTime;
    protected int _patrolIndex;
    protected bool _hasTrigger = false;

    protected Vector3 _defaultPosition;
    private Quaternion _defaultRotation;

    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _walkClip;
    [SerializeField] private AudioClip _runClip;
    [SerializeField] private AudioClip _attackClip;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        playerTransform = GameObject.FindWithTag("Player").transform;
        _defaultPosition = transform.position;
        _defaultRotation = transform.rotation;

        if(_enemyTrigger != null)
        {
            _enemyTrigger.Initialize(this);
        }

        ChangeState(defaultState);
    }

    private void Update()
    {
        switch (currentState)
        {
            case EnemyState.Idle:
                IdleBehavior();
                break;

            case EnemyState.Patrol:
                PatrolBehavior();
                break;

            case EnemyState.Chasing:
                ChaseBehavior();
                break;

            case EnemyState.Interacting:
                InteractBehavior();
                break;
        }

        CheckTransitions();
    }

    public void ChangeState(EnemyState nextState)
    {
        if(currentState != nextState)
        {
            currentState = nextState;
            switch (currentState)
            {
                case EnemyState.Idle:
                    //_audioSource.Stop();
                    _animator.SetTrigger("Idle");
                    IdleBehavior();
                    break;

                case EnemyState.Patrol:
                    //_audioSource.clip = _walkClip;
                    //_audioSource.Play();
                    _animator.SetTrigger("Walk");
                    PatrolBehavior();
                    break;

                case EnemyState.Chasing:
                    //_audioSource.clip = _runClip;
                    //_audioSource.Play();
                    agent.stoppingDistance = stopDistanceAttack;
                    _animator.SetTrigger("Run");
                    ChaseBehavior();
                    break;

                case EnemyState.Interacting:
                    _audioSource.Stop();
                    _audioSource.PlayOneShot(_attackClip);
                    _animator.SetTrigger("Interacting");
                    InteractBehavior();
                    break;
            }
        }
    }

    public abstract void CheckTransitions();

    protected void BacktoPlaceBehaviour()
    {
        agent.stoppingDistance = 0;
        agent.isStopped = false;
        agent.destination = _defaultPosition;
        RotateToTarget(_defaultPosition);

        if (agent.remainingDistance < 0.1f)
        {
            ChangeState(EnemyState.Idle);
            agent.isStopped = true;
            StartCoroutine(RotateBackThenIdle());
        }
    }

    public void EnemyTriggerEnter(bool isTrigger)
    {
        _hasTrigger = isTrigger;

        if (_hasTrigger)
        {
            HasTriggerEnterEnemy();
        }
        else
        {
            HasTriggerExitEnemy();
        }
    }

    public virtual void HasTriggerEnterEnemy()
    {

    }

    public virtual void HasTriggerExitEnemy()
    {

    }

    public void PlayWalkAudio()
    {
        _audioSource.PlayOneShot(_walkClip);
    }

    private IEnumerator RotateBackThenIdle()
    {
        while (Quaternion.Angle(transform.rotation, _defaultRotation) > 1f)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                _defaultRotation,
                360f * Time.deltaTime
            );
            yield return null;
        }

    }

    private void IdleBehavior()
    {
        agent.isStopped = true;
    }

    private void PatrolBehavior()
    {
        agent.stoppingDistance = 0;
        agent.isStopped = _patrolIdle;
        agent.destination = _patrolPoints[_patrolIndex].position;
        RotateToTarget(_patrolPoints[_patrolIndex].position);

        if (!agent.pathPending && agent.remainingDistance < 0.5f && !_patrolIdle)
        {
            _patrolIdle = true;
            _currentIdleTime = _idleTime;
            _animator.SetTrigger("Idle");
            _patrolIndex = (_patrolIndex + 1) % _patrolPoints.Length;
        }

        if (_patrolIdle)
        {
            _currentIdleTime -= Time.deltaTime;

            if(_currentIdleTime <= 0)
            {
                _patrolIndex = (_patrolIndex + 1) % _patrolPoints.Length;
                _patrolIdle = false;
                _animator.SetTrigger("Walk");
            }
        }
    }

    private void ChaseBehavior()
    {
        agent.isStopped = false;
        agent.destination = playerTransform.position;
        RotateToTarget(playerTransform.position);
    }

    private void RotateToTarget(Vector3 target)
    {
        Vector3 direction = target - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
            return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void InteractBehavior()
    {
        agent.isStopped = true;
    }
}

public enum EnemyState
{
    Idle,
    Patrol,
    Chasing,
    Interacting,
}

