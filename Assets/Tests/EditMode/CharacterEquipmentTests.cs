using CombatSystem.Character;
using CombatSystem.Weapons;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Utils;

namespace CombatSystem.Tests.EditMode
{
    public class CharacterEquipmentTests
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";

        private GameObject player;
        private CharacterEquipment equipment;
        private Transform weaponSocket;
        private Weapon weaponA;
        private Weapon weaponB;

        [SetUp]
        public void SetUp()
        {
            // 소켓 연결까지 검증하려고 실제 프리팹을 쓴다. EditMode 에서는 Awake 가 불리지 않으므로 Awake 에 의존하지 않는다.
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            equipment = player.GetComponent<CharacterEquipment>();
            Assert.That(equipment, Is.Not.Null, "Player 프리팹에 CharacterEquipment 가 없다.");

            SerializedObject serializedEquipment = new SerializedObject(equipment);
            weaponSocket = (Transform)serializedEquipment.FindProperty("weaponSocket").objectReferenceValue;
            Assert.That(weaponSocket, Is.Not.Null, "CharacterEquipment 의 weaponSocket 이 연결되지 않았다.");

            weaponA = CreateWeapon("WeaponA");
            weaponB = CreateWeapon("WeaponB");
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;

            // 무기가 소켓 아래에 붙어 있으면 player 와 함께 파괴된다.
            Object.DestroyImmediate(player);
            if (weaponA != null)
            {
                Object.DestroyImmediate(weaponA.gameObject);
            }

            if (weaponB != null)
            {
                Object.DestroyImmediate(weaponB.gameObject);
            }
        }

        [Test]
        public void Equip_SetsEquippedWeapon()
        {
            equipment.Equip(weaponA);

            Assert.That(equipment.EquippedWeapon, Is.SameAs(weaponA));
        }

        [Test]
        public void Equip_AttachesWeaponToSocket()
        {
            // 소켓과 다른 위치에 있던 무기가 소켓 위치로 옮겨지는지 보려고 월드 원점이 아닌 곳에 둔다.
            weaponA.transform.position = new Vector3(5f, 6f, 7f);

            equipment.Equip(weaponA);

            Assert.That(weaponA.transform.parent, Is.SameAs(weaponSocket));
            Assert.That(weaponSocket.parent.name, Is.EqualTo("Hand_R"));
            Assert.That(weaponA.transform.position, Is.EqualTo(weaponSocket.position).Using(Vector3EqualityComparer.Instance));
        }

        [Test]
        public void Equip_WhenAnotherWeaponIsEquipped_ReplacesEquippedWeapon()
        {
            equipment.Equip(weaponA);
            equipment.Equip(weaponB);

            Assert.That(equipment.EquippedWeapon, Is.SameAs(weaponB));
            Weapon[] weaponsInSocket = weaponSocket.GetComponentsInChildren<Weapon>();
            Assert.That(weaponsInSocket, Has.Length.EqualTo(1));
            Assert.That(weaponsInSocket[0], Is.SameAs(weaponB));
        }

        [Test]
        public void Equip_WhenAnotherWeaponIsEquipped_DetachesPreviousWeapon()
        {
            equipment.Equip(weaponA);
            equipment.Equip(weaponB);

            Assert.That(weaponA.transform.IsChildOf(weaponSocket), Is.False);
        }

        [Test]
        public void Equip_WhenSameWeaponIsEquipped_KeepsWeaponEquipped()
        {
            equipment.Equip(weaponA);
            equipment.Equip(weaponA);

            Assert.That(equipment.EquippedWeapon, Is.SameAs(weaponA));
            Assert.That(weaponA.transform.parent, Is.SameAs(weaponSocket));
        }

        [Test]
        public void Equip_WithNullWeapon_DoesNotChangeEquippedWeapon()
        {
            equipment.Equip(weaponA);

            // 거부하면서 남기는 오류 로그를 Test Runner 가 실패로 처리하지 않게 한다. 로그 자체는 검증하지 않는다.
            LogAssert.ignoreFailingMessages = true;
            equipment.Equip(null);

            Assert.That(equipment.EquippedWeapon, Is.SameAs(weaponA));
            Assert.That(weaponA.transform.parent, Is.SameAs(weaponSocket));
        }

        private static Weapon CreateWeapon(string name)
        {
            return new GameObject(name).AddComponent<Weapon>();
        }
    }
}
