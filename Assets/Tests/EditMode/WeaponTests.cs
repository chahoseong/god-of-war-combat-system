using System.Linq;
using CombatSystem.Character;
using CombatSystem.Weapons;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace CombatSystem.Tests.EditMode
{
    public class WeaponTests
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string SwordPrefabPath = "Assets/Prefabs/Weapons/Sword.prefab";
        private const string SwordSetPath = "Assets/Animations/Weapons/SwordAnimationSet.overrideController";

        private const string SpeedParameter = "Speed";
        private const string SwordArmLayer = "SwordArm";
        private const string SwordHandLayer = "SwordHand";

        private GameObject player;
        private Animator animator;
        private AnimatorOverrideController testSet;
        private GameObject weaponObject;
        private GameObject dummy;

        [SetUp]
        public void SetUp()
        {
            // EditMode 에서는 Awake/LateUpdate 가 불리지 않는다. Speed 는 테스트가 직접 넣고 Animator 를 직접 갱신한다.
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            CharacterAnimation characterAnimation = player.GetComponent<CharacterAnimation>();
            Assert.That(characterAnimation, Is.Not.Null, "Player 프리팹에 CharacterAnimation 이 없다.");

            SerializedObject serializedAnimation = new SerializedObject(characterAnimation);
            animator = (Animator)serializedAnimation.FindProperty("animator").objectReferenceValue;
            Assert.That(animator, Is.Not.Null, "CharacterAnimation 의 animator 가 연결되지 않았다.");

            // EditMode 에는 보이는 렌더러가 없어서 기본 설정(Cull Update Transforms)에서는 클립 정보가 갱신되지 않는다.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();

            // 검의 정식 세트와 구분되는 시험용 세트. 무기가 자기 세트를 전달하는지 인스턴스로 확인하려고 쓴다.
            testSet = new AnimatorOverrideController(animator.runtimeAnimatorController);
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(testSet);
            if (weaponObject != null)
            {
                Object.DestroyImmediate(weaponObject);
            }

            if (dummy != null)
            {
                Object.DestroyImmediate(dummy);
            }
        }

        // AC-1: 무기 A가 장착되면 애니메이션 시스템은 A의 세트를 전달받는다.
        [Test]
        public void OnEquipped_DeliversWeaponsOwnSetToCharacterAnimation()
        {
            Weapon weapon = CreateWeapon(testSet);

            weapon.OnEquipped(player);

            Assert.That(animator.runtimeAnimatorController, Is.SameAs(testSet));
        }

        // AC-1: 실제 검 프리팹의 세트가 지정되어 있고, 그 세트가 대기 클립 세 군데를 함께 바꾼다.
        [Test]
        public void OnEquipped_WithSwordPrefab_PlaysSwordIdleOnAllLayers()
        {
            // 장착 전에는 기본 대기 클립이어야, 장착 뒤의 변화가 세트 때문이라고 말할 수 있다.
            Evaluate(0f);
            Assert.That(PlayingClipNames(0), Is.EquivalentTo(new[] { "A_Idle_Standing_Masc" }), "장착 전 대기 클립이 아니다.");
            Weapon sword = InstantiateSword();

            sword.OnEquipped(player);
            Evaluate(0f);

            Assert.That(animator.runtimeAnimatorController,
                Is.SameAs(AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(SwordSetPath)));
            Assert.That(PlayingClipNames(0), Is.EquivalentTo(new[] { "A_Idle_Base_Sword" }));
            Assert.That(PlayingClipNames(animator.GetLayerIndex(SwordArmLayer)), Is.EquivalentTo(new[] { "A_Idle_Base_Sword" }));
            Assert.That(PlayingClipNames(animator.GetLayerIndex(SwordHandLayer)), Is.EquivalentTo(new[] { "A_Idle_Base_Sword" }));
        }

        [Test]
        public void OnEquipped_WithoutAnimationSet_DoesNotChangeController()
        {
            Weapon weapon = CreateWeapon(null);
            RuntimeAnimatorController before = animator.runtimeAnimatorController;

            // 거부하면서 남기는 오류 로그를 Test Runner 가 실패로 처리하지 않게 한다. 로그 자체는 검증하지 않는다.
            LogAssert.ignoreFailingMessages = true;
            weapon.OnEquipped(player);

            Assert.That(animator.runtimeAnimatorController, Is.SameAs(before));
            Assert.That(animator.GetLayerWeight(animator.GetLayerIndex(SwordArmLayer)), Is.EqualTo(0f));
        }

        [Test]
        public void OnEquipped_WithNullCharacter_DoesNotThrow()
        {
            Weapon weapon = CreateWeapon(testSet);

            LogAssert.ignoreFailingMessages = true;

            Assert.DoesNotThrow(() => weapon.OnEquipped(null));
        }

        [Test]
        public void OnEquipped_WhenCharacterHasNoCharacterAnimation_DoesNotThrow()
        {
            Weapon weapon = CreateWeapon(testSet);
            dummy = new GameObject("Dummy");

            LogAssert.ignoreFailingMessages = true;

            Assert.DoesNotThrow(() => weapon.OnEquipped(dummy));
        }

        private Weapon CreateWeapon(AnimatorOverrideController animationSet)
        {
            weaponObject = new GameObject("Weapon");
            Weapon weapon = weaponObject.AddComponent<Weapon>();

            SerializedObject serializedWeapon = new SerializedObject(weapon);
            serializedWeapon.FindProperty("animationSet").objectReferenceValue = animationSet;
            serializedWeapon.ApplyModifiedPropertiesWithoutUndo();
            return weapon;
        }

        private Weapon InstantiateSword()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SwordPrefabPath);
            weaponObject = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            Weapon sword = weaponObject.GetComponent<Weapon>();
            Assert.That(sword, Is.Not.Null, "Sword 프리팹에 Weapon 이 없다.");
            return sword;
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
