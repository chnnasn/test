// //Copyright 2022, Infima Games. All Rights Reserved.
//
// using UnityEngine;
//
// namespace InfimaGames.LowPolyShooterPack
// {
//     public class FeelModifier : Interactable
//     {
//         [SerializeField]
//         private FeelPreset feelPreset;
//
//         [SerializeField]
//         private Animator pivotAnimator;
//
//         /// <summary>
//         /// 交互。
//         /// </summary>
//         public override void Interact(GameObject actor = null)
//         {
//             //TODO: 清理整个组件。
//             if(pivotAnimator != null)
//                 pivotAnimator.Play("Press");
//
//             //需要actor参数以便将当前对象挂载到actor下。
//             if (actor != null)
//             {
//                 //获取角色行为组件，确保尝试拾取武器的是合法的角色。
//                 var characterBehaviour = actor.GetComponent<CharacterBehaviour>();
//                 if (characterBehaviour == null)
//                     return;
//
//                 var feelManager = actor.GetComponent<FeelManager>();
//                 if (feelManager != null)
//                 {
//                     feelManager.Preset = feelPreset;
//                 }
//             }
//         }
//     }
// }