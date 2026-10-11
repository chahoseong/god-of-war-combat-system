using CombatSystem.Character;
using UnityEngine;

namespace CombatSystem.Weapons
{
    // 무기는 자신의 애니메이션 세트를 들고 있다가, 장착 통지를 받으면 그 캐릭터의 애니메이션 시스템에 전달한다.
    // 세트가 어느 레이어에 어떻게 적용되는지는 CharacterAnimation 의 일이라서 여기서는 알지 못한다.
    public class Weapon : MonoBehaviour
    {
        [SerializeField] private AnimatorOverrideController animationSet;

        // 캐릭터를 GameObject 로 받는 것은 장비 시스템이 애니메이션 시스템을 모른 채 자기 gameObject 만 넘기게 하려는 것이다.
        public void OnEquipped(GameObject character)
        {
            // 상태를 바꾸기 전에 거른다.
            if (character == null)
            {
                Debug.LogError("무기가 장착된 캐릭터가 없습니다.", this);
                return;
            }

            // CharacterAnimation 에게 null 을 넘기면 로그가 캐릭터를 가리킨다. 설정은 무기에서 고쳐야 하므로 여기서 거른다.
            if (animationSet == null)
            {
                Debug.LogError("무기에 애니메이션 세트가 지정되지 않았습니다.", this);
                return;
            }

            if (!character.TryGetComponent(out CharacterAnimation characterAnimation))
            {
                Debug.LogError($"캐릭터 '{character.name}' 에 CharacterAnimation 이 없어 애니메이션 세트를 전달할 수 없습니다.", this);
                return;
            }

            characterAnimation.ApplyAnimationSet(animationSet);
        }
    }
}
