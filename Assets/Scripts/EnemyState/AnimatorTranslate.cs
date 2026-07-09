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
    GetHit
}

public class AnimatorTranslate : StateMachineBehaviour
{
    Enemy enemy;

    [SerializeField] public  AnimationState onEnterAnimationState;
        
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        if (onEnterAnimationState == AnimationState.Null)
        {
            return;
        }

        if (animator.TryGetComponent<Enemy>(out enemy))
        {
            enemy.OnAnimationEnterEvent(onEnterAnimationState, stateInfo.length);
        }
    }


    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        
        if (onEnterAnimationState == AnimationState.Null)
        {
            return;
        }

        if (animator.TryGetComponent<Enemy>(out enemy))
        {
            enemy.OnAnimationExitEvent(onEnterAnimationState);
        }
    }
    
    
    

}