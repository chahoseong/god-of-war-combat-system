using System;
using UnityEngine;

namespace CombatSystem.Character
{
    [RequireComponent(typeof(CharacterMovement))]
    public class CharacterAnimation : MonoBehaviour
    {
        [Serializable]
        private struct OverrideLayer
        {
            public string name;
            public float weight;
        }

        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private const string AttackLayerName = "Attack";

        private static readonly int AttackHash = Animator.StringToHash("Attack");

        [SerializeField] private Animator animator;

        // 런타임에는 AnimatorController 의 레이어 설정을 읽을 수 없어서, 세트를 적용할 때 켤 레이어와 가중치를 여기에 둔다.
        [SerializeField] private OverrideLayer[] overrideLayers =
        {
            new OverrideLayer { name = "SwordArm", weight = 0.65f },
            new OverrideLayer { name = "SwordHand", weight = 1f },
        };

        private CharacterMovement movement;

        // 세트는 Animator 의 컨트롤러에 있는 클립을 바꿔 끼우는 방식으로 적용된다.
        public void ApplyAnimationSet(AnimatorOverrideController animationSet)
        {
            // 상태를 바꾸기 전에 거른다. 컨트롤러만 바뀌고 레이어는 꺼진 채 남지 않게 한다.
            if (animationSet == null)
            {
                Debug.LogError("적용할 애니메이션 세트가 없습니다.", this);
                return;
            }

            animator.runtimeAnimatorController = animationSet;

            // 컨트롤러를 바꾸면 레이어 가중치가 기본값(0)으로 돌아가므로, 바꾼 뒤에 켠다.
            foreach (OverrideLayer overrideLayer in overrideLayers)
            {
                int layerIndex = animator.GetLayerIndex(overrideLayer.name);
                if (layerIndex < 0)
                {
                    Debug.LogError($"애니메이션 세트를 덮어쓸 레이어 '{overrideLayer.name}' 이 컨트롤러에 없습니다.", this);
                    continue;
                }

                animator.SetLayerWeight(layerIndex, overrideLayer.weight);
            }
        }

        // 공격 레이어는 공격 상태에 머무는 동안에만 켜져 있고, 끄는 것은 EnableLayerWhilePlaying 이 맡는다. 그래서 overrideLayers 에 두지 않는다.
        // 한 번만 재생하고 끝나는 것도, 재생 중 재요청에 처음부터 다시 시작하는 것도 컨트롤러의 전환이 맡는다.
        public void PlayAttackAnimation()
        {
            int layerIndex = animator.GetLayerIndex(AttackLayerName);
            if (layerIndex < 0)
            {
                Debug.LogError($"공격 애니메이션을 재생할 레이어 '{AttackLayerName}' 이 컨트롤러에 없습니다.", this);
                return;
            }

            // 꺼진 레이어는 그 프레임에 평가되지 않는다. 공격 상태가 켜 주기를 기다리면 첫 프레임이 공격 포즈 없이 지나가므로 미리 켠다.
            animator.SetLayerWeight(layerIndex, 1f);
            animator.SetTrigger(AttackHash);
        }

        private void Awake()
        {
            movement = GetComponent<CharacterMovement>();
        }

        // CharacterMovement가 Update에서 몸을 옮긴 뒤의 속도를 읽는다.
        private void LateUpdate()
        {
            Vector3 velocity = movement.Velocity;
            velocity.y = 0f;
            animator.SetFloat(SpeedHash, velocity.magnitude);
        }
    }
}
