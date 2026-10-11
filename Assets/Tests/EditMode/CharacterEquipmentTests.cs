using System.Linq;
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
        private const string SwordPrefabPath = "Assets/Prefabs/Weapons/Sword.prefab";

        private const string SpeedParameter = "Speed";
        private const string SwordArmLayer = "SwordArm";
        private const string SwordHandLayer = "SwordHand";

        private GameObject player;
        private CharacterEquipment equipment;
        private Transform weaponSocket;
        private Animator animator;
        private AnimatorOverrideController setA;
        private AnimatorOverrideController setB;
        private Weapon weaponA;
        private Weapon weaponB;
        private GameObject sword;

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

            CharacterAnimation characterAnimation = player.GetComponent<CharacterAnimation>();
            Assert.That(characterAnimation, Is.Not.Null, "Player 프리팹에 CharacterAnimation 이 없다.");

            SerializedObject serializedAnimation = new SerializedObject(characterAnimation);
            animator = (Animator)serializedAnimation.FindProperty("animator").objectReferenceValue;
            Assert.That(animator, Is.Not.Null, "CharacterAnimation 의 animator 가 연결되지 않았다.");

            // EditMode 에는 보이는 렌더러가 없어서 기본 설정(Cull Update Transforms)에서는 클립 정보가 갱신되지 않는다.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();

            // 무기가 정상이어야 장착이 에러 없이 끝난다. 세트는 어느 무기의 것인지 구별되도록 서로 다른 인스턴스로 만든다.
            // 장착 전의 컨트롤러를 바탕으로 만들어야 하므로 둘 다 장착 전에 만든다.
            setA = new AnimatorOverrideController(animator.runtimeAnimatorController);
            setB = new AnimatorOverrideController(animator.runtimeAnimatorController);
            weaponA = CreateWeapon("WeaponA", setA);
            weaponB = CreateWeapon("WeaponB", setB);
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;

            // 무기가 소켓 아래에 붙어 있으면 player 와 함께 파괴된다.
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(setA);
            Object.DestroyImmediate(setB);
            if (weaponA != null)
            {
                Object.DestroyImmediate(weaponA.gameObject);
            }

            if (weaponB != null)
            {
                Object.DestroyImmediate(weaponB.gameObject);
            }

            if (sword != null)
            {
                Object.DestroyImmediate(sword);
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

        // AC-5: Weapon 에는 통지받았는지 볼 수 있는 상태가 없다. 통지를 받으면 무기가 자기 세트를 그 캐릭터의
        // Animator 에 전달하므로, 컨트롤러가 그 무기의 세트로 바뀌었는지로 통지가 갔음을 확인한다.
        [Test]
        public void Equip_NotifiesWeaponOfCharacter()
        {
            equipment.Equip(weaponA);

            Assert.That(animator.runtimeAnimatorController, Is.SameAs(setA));
        }

        [Test]
        public void Equip_WhenAnotherWeaponIsEquipped_NotifiesNewWeapon()
        {
            equipment.Equip(weaponA);
            equipment.Equip(weaponB);

            Assert.That(animator.runtimeAnimatorController, Is.SameAs(setB));
        }

        [Test]
        public void Equip_WhenSameWeaponIsEquipped_DoesNotNotifyAgain()
        {
            equipment.Equip(weaponA);
            // 다시 통지가 갔다면 A 의 세트로 되돌아간다. 그러지 않았음을 보려고 다른 세트로 바꿔 둔다.
            animator.runtimeAnimatorController = setB;

            equipment.Equip(weaponA);

            Assert.That(animator.runtimeAnimatorController, Is.SameAs(setB));
        }

        // 장비 → 무기 → 애니메이션 시스템이 이어진 흐름을 실제 검 프리팹으로 확인한다.
        [Test]
        public void Equip_WithSwordPrefab_PlaysSwordIdleOnAllLayers()
        {
            GameObject swordPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPrefabPath);
            sword = (GameObject)PrefabUtility.InstantiatePrefab(swordPrefab);
            Weapon swordWeapon = sword.GetComponent<Weapon>();
            Assert.That(swordWeapon, Is.Not.Null, "Sword 프리팹에 Weapon 이 없다.");

            equipment.Equip(swordWeapon);
            Evaluate(0f);

            Assert.That(PlayingClipNames(0), Is.EquivalentTo(new[] { "A_Idle_Base_Sword" }));
            Assert.That(PlayingClipNames(animator.GetLayerIndex(SwordArmLayer)), Is.EquivalentTo(new[] { "A_Idle_Base_Sword" }));
            Assert.That(PlayingClipNames(animator.GetLayerIndex(SwordHandLayer)), Is.EquivalentTo(new[] { "A_Idle_Base_Sword" }));
        }

        private static Weapon CreateWeapon(string name, AnimatorOverrideController animationSet)
        {
            Weapon weapon = new GameObject(name).AddComponent<Weapon>();

            SerializedObject serializedWeapon = new SerializedObject(weapon);
            serializedWeapon.FindProperty("animationSet").objectReferenceValue = animationSet;
            serializedWeapon.ApplyModifiedPropertiesWithoutUndo();
            return weapon;
        }

        private void Evaluate(float speed)
        {
            animator.SetFloat(SpeedParameter, speed);
            animator.Update(0.1f);
        }

        private string[] PlayingClipNames(int layer)
        {
            return animator.GetCurrentAnimatorClipInfo(layer)
                .Where(info => info.weight > 0f)
                .Select(info => info.clip.name)
                .ToArray();
        }
    }
}
