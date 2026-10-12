using UnityEngine;

namespace CombatSystem.Character
{
    // 이 상태에 머무는 동안에만 상태가 속한 레이어를 켠다.
    // 휴머노이드의 Override 레이어는 빈 상태여도 가중치가 1 이면 아래 레이어를 기본 포즈로 덮어쓰므로,
    // 재생하지 않을 때는 가중치로 레이어를 꺼 두어야 한다.
    public class EnableLayerWhilePlaying : StateMachineBehaviour
    {
        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.SetLayerWeight(layerIndex, 1f);
        }

        public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animator.SetLayerWeight(layerIndex, 0f);
        }
    }
}
