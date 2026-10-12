using UnityEngine;

namespace CombatSystem.Character
{
    // 공격 요청을 애니메이션 시스템에 그대로 전달한다. 공격 중인지는 기억하지도 확인하지도 않는다.
    // 재생 중 재요청에 처음부터 다시 시작하는 것은 애니메이션 시스템이 맡는다.
    [RequireComponent(typeof(CharacterAnimation))]
    public class CharacterAttack : MonoBehaviour
    {
        public void Attack()
        {
            // Awake 에서 받아 두지 않는다. Attack 이 Awake 이전에 불려도 동작해야 한다.
            GetComponent<CharacterAnimation>().PlayAttackAnimation();
        }
    }
}
