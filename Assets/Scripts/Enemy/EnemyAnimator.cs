using UnityEngine;

[RequireComponent(typeof(Animator))]
public class EnemyAnimator : MonoBehaviour
{
    private static readonly int ChaseStateHash = Animator.StringToHash("ChaseState");
    private const string DeadStateName = "Dead";
    private const string GetHitStateName = "GeiHit";

    private Enemy _enemy;
    private Animator _animator;
    private float _deadAnimationDuration;

    public float DeadAnimationDuration => _deadAnimationDuration;
    public bool HasAnimator => _animator != null;

    private void Awake()
    {
        _enemy = GetComponent<Enemy>();
        _animator = GetComponent<Animator>();
        if (_animator != null)
            _animator.applyRootMotion = false;
    }

    public void SetChaseState(float value)
    {
        if (_animator == null) return;
        _animator.SetFloat(ChaseStateHash, value);
    }

    public void PlayDead()
    {
        if (_animator == null) return;
        _animator.Play(DeadStateName, 0, 0f);
    }

    public void PlayGetHit()
    {
        if (_animator == null || _enemy == null || _enemy.IsDying) return;
        _animator.Play(GetHitStateName, 0, 0f);
    }

    public void OnAnimationEnterEvent(AnimationState playerState, float animationLength = 0f)
    {
        if (_enemy == null) return;

        switch (playerState)
        {
            case AnimationState.Attack:
                _enemy.OnAttackAnimationEvent();
                break;
            case AnimationState.dead:
                _deadAnimationDuration = animationLength;
                _enemy.stateMachine.OnAnimationTranslateEvent(_enemy.stateMachine.deadState);
                break;
        }
    }

    public void OnAnimationExitEvent(AnimationState playerState)
    {
        if (_enemy == null) return;

        switch (playerState)
        {
            case AnimationState.Birth:
            case AnimationState.GetHit:
                _enemy.stateMachine.OnAnimationTranslateEvent(_enemy.stateMachine.chaseState);
                break;
        }
    }
}
