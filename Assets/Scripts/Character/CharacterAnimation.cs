using UnityEngine;

namespace CombatSystem.Character
{
    [RequireComponent(typeof(CharacterMovement))]
    public class CharacterAnimation : MonoBehaviour
    {
        private static readonly int SpeedHash = Animator.StringToHash("Speed");

        [SerializeField] private Animator animator;

        private CharacterMovement movement;

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
