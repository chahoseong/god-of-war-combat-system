using CombatSystem.Weapons;
using UnityEngine;

namespace CombatSystem.Character
{
    public class CharacterEquipment : MonoBehaviour
    {
        [SerializeField] private Transform weaponSocket;

        private Weapon equippedWeapon;

        public Weapon EquippedWeapon => equippedWeapon;

        public void Equip(Weapon weapon)
        {
            // 상태를 바꾸기 전에 거른다. 이전 무기를 뗀 뒤에 실패하면 장착 상태가 반쯤 바뀐 채 남는다.
            if (weapon == null)
            {
                Debug.LogError("장착할 무기가 없습니다.", this);
                return;
            }

            if (weapon == equippedWeapon)
            {
                return;
            }

            if (equippedWeapon != null)
            {
                equippedWeapon.transform.SetParent(null);
            }

            weapon.transform.SetParent(weaponSocket, false);
            weapon.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            equippedWeapon = weapon;
        }
    }
}
