using CombatSystem.Character;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CombatSystem.Player
{
    [RequireComponent(typeof(CharacterMovement))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference sprintAction;
        [SerializeField] private Transform cameraTransform;

        private CharacterMovement movement;

        private void Awake()
        {
            movement = GetComponent<CharacterMovement>();
        }

        private void Update()
        {
            Vector2 input = moveAction.action.ReadValue<Vector2>();
            Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
            Vector3 right = Vector3.ProjectOnPlane(cameraTransform.right, Vector3.up).normalized;
            Vector3 direction = forward * input.y + right * input.x;
            bool sprint = sprintAction.action.IsPressed();

            movement.Move(direction, sprint);
        }
    }
}
