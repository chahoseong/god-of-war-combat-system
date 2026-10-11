using System.Linq;
using CombatSystem.Character;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;

namespace CombatSystem.Tests.EditMode
{
    public class CharacterAnimationTests
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string ControllerPath = "Assets/Animations/Character/Player.controller";
        private const string ArmMaskPath = "Assets/Animations/Character/RightArmMask.mask";
        private const string HandMaskPath = "Assets/Animations/Character/RightHandMask.mask";

        private const string IdleFbxPath =
            "Assets/ThirdParty/Synty/AnimationBaseLocomotion/Animations/Polygon/Masculine/Idle/A_Idle_Standing_Masc.fbx";

        private const string SwordIdleFbxPath =
            "Assets/ThirdParty/Synty/AnimationSwordCombat/Animations/Polygon/Idle/Base/A_Idle_Base_Sword.fbx";

        private const string Prop_RPath =
            "Root/Hips/Spine_01/Spine_02/Spine_03/Clavicle_R/Shoulder_R/Elbow_R/Hand_R/Prop_R";

        private const string SpeedParameter = "Speed";
        private const string SwordArmLayer = "SwordArm";
        private const string SwordHandLayer = "SwordHand";

        // Player.controller 의 Locomotion 블렌드 트리 임계값. 임계값에서는 해당 클립 하나만 재생된다.
        private const float IdleSpeed = 0f;
        private const float WalkSpeed = 1.46f;
        private const float RunSpeed = 2.6f;
        private const float SprintSpeed = 7.27f;

        // 세트가 상체에 적용될 때 바뀌어야 하는 본과, 바뀌지 않아야 하는 본.
        private static readonly string[] RightArmBones = { "Shoulder_R", "Elbow_R", "Hand_R" };

        private static readonly string[] UnaffectedBones =
        {
            "Hips", "Spine_01", "Spine_02", "Spine_03", "Neck", "Head",
            "Clavicle_L", "Shoulder_L", "Elbow_L", "Hand_L",
            "UpperLeg_L", "LowerLeg_L", "Ankle_L", "Ball_L", "Toes_L",
            "UpperLeg_R", "LowerLeg_R", "Ankle_R", "Ball_R", "Toes_R",
        };

        private GameObject player;
        private CharacterAnimation characterAnimation;
        private Animator animator;
        private AnimationClip idleClip;
        private AnimationClip swordIdleClip;
        private AnimatorOverrideController animationSet;

        [SetUp]
        public void SetUp()
        {
            // EditMode 에서는 Awake/LateUpdate 가 불리지 않는다. Speed 는 테스트가 직접 넣고 Animator 를 직접 갱신한다.
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            characterAnimation = player.GetComponent<CharacterAnimation>();
            Assert.That(characterAnimation, Is.Not.Null, "Player 프리팹에 CharacterAnimation 이 없다.");

            SerializedObject serializedAnimation = new SerializedObject(characterAnimation);
            animator = (Animator)serializedAnimation.FindProperty("animator").objectReferenceValue;
            Assert.That(animator, Is.Not.Null, "CharacterAnimation 의 animator 가 연결되지 않았다.");

            idleClip = LoadClip(IdleFbxPath);
            swordIdleClip = LoadClip(SwordIdleFbxPath);

            // 시험용 세트: 대기 클립을 검 대기 클립으로 바꾼다. 검의 정식 세트 에셋은 무기 쪽에서 만든다.
            animationSet = new AnimatorOverrideController(animator.runtimeAnimatorController);
            animationSet[idleClip] = swordIdleClip;

            // EditMode 에는 보이는 렌더러가 없어서 기본 설정(Cull Update Transforms)에서는 본의 트랜스폼이 갱신되지 않는다.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;

            Object.DestroyImmediate(player);
            Object.DestroyImmediate(animationSet);
        }

        // AC-1
        [TestCase(IdleSpeed, "A_Idle_Standing_Masc")]
        [TestCase(WalkSpeed, "A_Walk_F_Masc")]
        [TestCase(RunSpeed, "A_Run_F_Masc")]
        [TestCase(SprintSpeed, "A_Sprint_F_Masc")]
        public void Locomotion_PlaysClipForSpeed(float speed, string expectedClip)
        {
            Evaluate(speed);

            Assert.That(PlayingClipNames(0), Is.EquivalentTo(new[] { expectedClip }));
        }

        // AC-1
        [Test]
        public void Locomotion_BetweenThresholds_BlendsNeighborClips()
        {
            Evaluate((WalkSpeed + RunSpeed) * 0.5f);

            Assert.That(PlayingClipNames(0), Is.EquivalentTo(new[] { "A_Walk_F_Masc", "A_Run_F_Masc" }));
        }

        // AC-2
        [Test]
        public void ApplyAnimationSet_WhenStopped_PlaysSetClip()
        {
            characterAnimation.ApplyAnimationSet(animationSet);
            Evaluate(IdleSpeed);

            Assert.That(PlayingClips(0), Is.EquivalentTo(new[] { swordIdleClip }));
        }

        // AC-3
        [Test]
        public void ApplyAnimationSet_WhenMoving_PlaysSetClipOnArmLayersAndLocomotionOnBase()
        {
            characterAnimation.ApplyAnimationSet(animationSet);
            Evaluate(RunSpeed);

            Assert.That(PlayingClipNames(0), Is.EquivalentTo(new[] { "A_Run_F_Masc" }));
            Assert.That(PlayingClips(animator.GetLayerIndex(SwordArmLayer)), Is.EquivalentTo(new[] { swordIdleClip }));
            Assert.That(PlayingClips(animator.GetLayerIndex(SwordHandLayer)), Is.EquivalentTo(new[] { swordIdleClip }));
            Assert.That(animator.GetLayerWeight(animator.GetLayerIndex(SwordArmLayer)), Is.GreaterThan(0f));
            Assert.That(animator.GetLayerWeight(animator.GetLayerIndex(SwordHandLayer)), Is.EqualTo(1f));
        }

        // AC-3: 세트가 바꾸는 것은 오른팔뿐이고, 몸통·왼팔·다리는 로코모션 그대로여야 한다.
        [Test]
        public void ApplyAnimationSet_WhenMoving_ChangesOnlyRightArmPose()
        {
            characterAnimation.ApplyAnimationSet(animationSet);
            Evaluate(RunSpeed);
            Quaternion[] withSetArm = Rotations(RightArmBones);
            Quaternion[] withSetOthers = Rotations(UnaffectedBones);
            Quaternion withSetProp = FindBone("Prop_R").localRotation;

            // 같은 시각의 포즈에서 두 레이어만 끈 것과 비교한다. 시간을 더 흘리면 로코모션 자체가 달라진다.
            animator.SetLayerWeight(animator.GetLayerIndex(SwordArmLayer), 0f);
            animator.SetLayerWeight(animator.GetLayerIndex(SwordHandLayer), 0f);
            animator.Update(0f);
            Quaternion[] locomotionArm = Rotations(RightArmBones);
            Quaternion[] locomotionOthers = Rotations(UnaffectedBones);
            Quaternion locomotionProp = FindBone("Prop_R").localRotation;

            for (int i = 0; i < UnaffectedBones.Length; i++)
            {
                Assert.That(Quaternion.Angle(locomotionOthers[i], withSetOthers[i]), Is.LessThan(0.01f),
                    $"{UnaffectedBones[i]} 이(가) 세트의 영향을 받았다.");
            }

            float armChange = withSetArm.Select((rotation, i) => Quaternion.Angle(locomotionArm[i], rotation)).Max();
            Assert.That(armChange, Is.GreaterThan(1f), "오른팔이 세트의 애니메이션으로 바뀌지 않았다.");

            // Prop_R 은 휴머노이드 본이 아니라서 트랜스폼 목록에 있어야만 레이어를 통해 움직인다.
            Assert.That(Quaternion.Angle(locomotionProp, withSetProp), Is.GreaterThan(0.1f),
                "Prop_R 이 레이어를 통해 움직이지 않았다.");
        }

        // AC-4
        [Test]
        public void WithoutAnimationSet_WhenMoving_PlaysLocomotionOnWholeBody()
        {
            Evaluate(RunSpeed);

            Assert.That(PlayingClipNames(0), Is.EquivalentTo(new[] { "A_Run_F_Masc" }));
            Assert.That(animator.GetLayerWeight(animator.GetLayerIndex(SwordArmLayer)), Is.EqualTo(0f));
            Assert.That(animator.GetLayerWeight(animator.GetLayerIndex(SwordHandLayer)), Is.EqualTo(0f));
        }

        [Test]
        public void ApplyAnimationSet_WithNullSet_DoesNotChangeController()
        {
            RuntimeAnimatorController before = animator.runtimeAnimatorController;

            // 거부하면서 남기는 오류 로그를 Test Runner 가 실패로 처리하지 않게 한다. 로그 자체는 검증하지 않는다.
            LogAssert.ignoreFailingMessages = true;
            characterAnimation.ApplyAnimationSet(null);

            Assert.That(animator.runtimeAnimatorController, Is.SameAs(before));
            Assert.That(animator.GetLayerWeight(animator.GetLayerIndex(SwordArmLayer)), Is.EqualTo(0f));
        }

        [Test]
        public void Controller_HasOverrideLayersForSwordArmAndHand()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);

            AnimatorControllerLayer arm = controller.layers.Single(layer => layer.name == SwordArmLayer);
            AnimatorControllerLayer hand = controller.layers.Single(layer => layer.name == SwordHandLayer);

            Assert.That(arm.blendingMode, Is.EqualTo(AnimatorLayerBlendingMode.Override));
            Assert.That(hand.blendingMode, Is.EqualTo(AnimatorLayerBlendingMode.Override));
            // 세트가 적용되기 전에는 꺼져 있어야 한다.
            Assert.That(arm.defaultWeight, Is.EqualTo(0f));
            Assert.That(hand.defaultWeight, Is.EqualTo(0f));
            Assert.That(AssetDatabase.GetAssetPath(arm.avatarMask), Is.EqualTo(ArmMaskPath));
            Assert.That(AssetDatabase.GetAssetPath(hand.avatarMask), Is.EqualTo(HandMaskPath));
        }

        [Test]
        public void RightArmMask_CoversRightArmAndProp_R()
        {
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(ArmMaskPath);

            AssertOnlyHumanoidParts(mask, AvatarMaskBodyPart.RightArm, AvatarMaskBodyPart.RightFingers, AvatarMaskBodyPart.RightHandIK);
            Assert.That(IsTransformActive(mask, Prop_RPath), Is.True);
            Assert.That(IsTransformActive(mask, "Root/Hips/UpperLeg_L/LowerLeg_L/Ankle_L/Ball_L/Toes_L"), Is.False);
        }

        [Test]
        public void RightHandMask_CoversRightHandAndProp_R()
        {
            AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(HandMaskPath);

            AssertOnlyHumanoidParts(mask, AvatarMaskBodyPart.RightFingers, AvatarMaskBodyPart.RightHandIK);
            Assert.That(IsTransformActive(mask, Prop_RPath), Is.True);
        }

        private void Evaluate(float speed)
        {
            animator.SetFloat(SpeedParameter, speed);
            animator.Update(0.1f);
        }

        private AnimationClip[] PlayingClips(int layer)
        {
            return animator.GetCurrentAnimatorClipInfo(layer)
                .Where(info => info.weight > 0f)
                .Select(info => info.clip)
                .ToArray();
        }

        private string[] PlayingClipNames(int layer)
        {
            return PlayingClips(layer).Select(clip => clip.name).ToArray();
        }

        private Transform FindBone(string boneName)
        {
            return player.GetComponentsInChildren<Transform>().First(t => t.name == boneName);
        }

        private Quaternion[] Rotations(string[] boneNames)
        {
            return boneNames.Select(boneName => FindBone(boneName).localRotation).ToArray();
        }

        private static AnimationClip LoadClip(string fbxPath)
        {
            // FBX 에는 미리보기용 클립이 함께 들어 있어서 이름으로 거른다.
            AnimationClip clip = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
                .OfType<AnimationClip>()
                .FirstOrDefault(c => !c.name.StartsWith("__preview__"));
            Assert.That(clip, Is.Not.Null, $"{fbxPath} 에서 클립을 찾지 못했다.");
            return clip;
        }

        private static void AssertOnlyHumanoidParts(AvatarMask mask, params AvatarMaskBodyPart[] expectedParts)
        {
            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                AvatarMaskBodyPart part = (AvatarMaskBodyPart)i;
                bool expected = expectedParts.Contains(part);
                Assert.That(mask.GetHumanoidBodyPartActive(part), Is.EqualTo(expected), part.ToString());
            }
        }

        private static bool IsTransformActive(AvatarMask mask, string path)
        {
            for (int i = 0; i < mask.transformCount; i++)
            {
                if (mask.GetTransformPath(i) == path)
                {
                    return mask.GetTransformActive(i);
                }
            }

            return false;
        }
    }
}
