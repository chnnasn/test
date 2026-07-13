using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public  enum AnimationState
{
    Null,
    Birth,
    chase,
    dead,
    Attack,
    GetHit,
    Boom
}

public class AnimatorTranslate : StateMachineBehaviour
{
    EnemyAnimator enemyAnimator;

    [SerializeField] public  AnimationState onEnterAnimationState;
        
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        if (onEnterAnimationState == AnimationState.Null)
        {
            return;
        }

        if (animator.TryGetComponent<EnemyAnimator>(out enemyAnimator))
        {
            enemyAnimator.OnAnimationEnterEvent(onEnterAnimationState, stateInfo.length);
        }
    }


    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        if (onEnterAnimationState == AnimationState.Null)
        {
            return;
        }

        if (animator.TryGetComponent<EnemyAnimator>(out enemyAnimator))
        {
            enemyAnimator.OnAnimationExitEvent(onEnterAnimationState);
        }
    }
    
    
    

}