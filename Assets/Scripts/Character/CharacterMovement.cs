using UnityEngine;

namespace CombatSystem.Character
{
    [RequireComponent(typeof(CharacterController))]
    [DefaultExecutionOrder(100)]
    public class CharacterMovement : MonoBehaviour
    {
        private const float GroundedVerticalVelocity = -2f;

        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private float sprintSpeed = 7f;
        [SerializeField] private float acceleration = 20f;
        [SerializeField] private float rotationSpeed = 720f;
        [SerializeField] private float gravity = -20f;

        private CharacterController controller;
        private Vector3 requestedDirection;
        private bool requestedSprint;
        private Vector3 horizontalVelocity;
        private float verticalVelocity;

        public Vector3 Velocity => controller.velocity;
        public bool IsGrounded => controller.isGrounded;

        // 호출한 프레임에만 유효하다. 다음 프레임에 다시 호출하지 않으면 감속해 멈춘다.
        public void Move(Vector3 direction, bool sprint)
        {
            direction.y = 0f;
            requestedDirection = Vector3.ClampMagnitude(direction, 1f);
            requestedSprint = sprint;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            UpdateHorizontalVelocity(deltaTime);
            UpdateRotation(deltaTime);
            UpdateVerticalVelocity(deltaTime);

            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * deltaTime);

            requestedDirection = Vector3.zero;
            requestedSprint = false;
        }

        private void UpdateHorizontalVelocity(float deltaTime)
        {
            float targetSpeed = requestedSprint ? sprintSpeed : moveSpeed;
            Vector3 targetVelocity = requestedDirection * targetSpeed;
            horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, acceleration * deltaTime);
        }

        private void UpdateRotation(float deltaTime)
        {
            if (requestedDirection == Vector3.zero)
            {
                return;
            }

            Quaternion targetRotation = Quaternion.LookRotation(requestedDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * deltaTime);
        }

        private void UpdateVerticalVelocity(float deltaTime)
        {
            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = GroundedVerticalVelocity;
            }

            verticalVelocity += gravity * deltaTime;
        }
    }
}
