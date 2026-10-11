using CombatSystem.Character;
using CombatSystem.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace CombatSystem.Player
{
    [RequireComponent(typeof(CharacterMovement))]
    [RequireComponent(typeof(CharacterEquipment))]
    public class PlayerController : MonoBehaviour
    {
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference sprintAction;
        [SerializeField] private InputActionReference lookAction;
        [SerializeField] private Transform cameraTarget;
        [SerializeField] private Weapon initialWeapon;
        [SerializeField] private float pointerSensitivity = 0.1f;
        [SerializeField] private float stickSensitivity = 180f;
        [SerializeField] private float minPitch = -30f;
        [SerializeField] private float maxPitch = 60f;

        private CharacterMovement movement;
        private CharacterEquipment equipment;
        private float yaw;
        private float pitch;

        private void Awake()
        {
            movement = GetComponent<CharacterMovement>();
            equipment = GetComponent<CharacterEquipment>();

            Vector3 angles = cameraTarget.rotation.eulerAngles;
            yaw = angles.y;
            pitch = Mathf.DeltaAngle(0f, angles.x);

            // 프리팹 에셋이므로 인스턴스를 만들어 장착한다. 지정하지 않으면 무기 없이 시작한다.
            if (initialWeapon != null)
            {
                equipment.Equip(Instantiate(initialWeapon));
            }
        }

        private void Update()
        {
            UpdateLook();
            UpdateMove();
        }

        // cameraTarget은 자식이라, 캐릭터가 Update에서 회전한 뒤에 정해야 그 회전에 끌려가지 않는다.
        private void LateUpdate()
        {
            cameraTarget.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void UpdateLook()
        {
            InputAction action = lookAction.action;
            Vector2 input = action.ReadValue<Vector2>();

            // Pointer의 delta는 프레임당 이동량이고, 스틱은 기울기라서 deltaTime을 곱해야 한다.
            bool isPointer = action.activeControl?.device is Pointer;
            float scale = isPointer ? pointerSensitivity : stickSensitivity * Time.deltaTime;

            yaw += input.x * scale;
            pitch = Mathf.Clamp(pitch - input.y * scale, minPitch, maxPitch);
        }

        private void UpdateMove()
        {
            Vector2 input = moveAction.action.ReadValue<Vector2>();
            Vector3 direction = Quaternion.Euler(0f, yaw, 0f) * new Vector3(input.x, 0f, input.y);
            bool sprint = sprintAction.action.IsPressed();

            movement.Move(direction, sprint);
        }
    }
}
