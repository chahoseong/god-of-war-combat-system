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

        private const string AttackPlaceholderPath = "Assets/Animations/Character/AttackPlaceholder.anim";

        private const string SwordAttackFbxPath =
            "Assets/ThirdParty/Synty/AnimationSwordCombat/Animations/Polygon/Attack/LightCombo01/A_Attack_LightCombo01A_ReturnToIdle_Sword.fbx";

        private const string Prop_RPath =
            "Root/Hips/Spine_01/Spine_02/Spine_03/Clavicle_R/Shoulder_R/Elbow_R/Hand_R/Prop_R";

        private const string SpeedParameter = "Speed";
        private const string SwordArmLayer = "SwordArm";
        private const string SwordHandLayer = "SwordHand";
        private const string AttackLayer = "Attack";

        // 한 번의 Update 로 진행시키는 시간. 길게 한 번에 진행시키면 상태 전환이 한 프레임에 몰린다.
        private const float Step = 0.1f;

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

        // 공격 애니메이션이 전신을 덮는지 보기 위해 몸을 상체와 하체로 나눈다.
        private static readonly string[] UpperBodyBones =
        {
            "Spine_01", "Spine_02", "Spine_03", "Neck", "Head",
            "Clavicle_L", "Shoulder_L", "Elbow_L", "Hand_L",
            "Clavicle_R", "Shoulder_R", "Elbow_R", "Hand_R",
        };

        private static readonly string[] LowerBodyBones =
        {
            "Hips",
            "UpperLeg_L", "LowerLeg_L", "Ankle_L", "Ball_L", "Toes_L",
            "UpperLeg_R", "LowerLeg_R", "Ankle_R", "Ball_R", "Toes_R",
        };

        private GameObject player;
        private CharacterAnimation characterAnimation;
        private Animator animator;
        private AnimationClip idleClip;
        private AnimationClip swordIdleClip;
        private AnimationClip attackPlaceholderClip;
        private AnimationClip swordAttackClip;
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
            swordAttackClip = LoadClip(SwordAttackFbxPath);

            attackPlaceholderClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackPlaceholderPath);
            Assert.That(attackPlaceholderClip, Is.Not.Null, $"{AttackPlaceholderPath} 가 없다.");

            // 시험용 세트: 대기 클립을 검 대기 클립으로, 공격 자리표시 클립을 검 공격 클립으로 바꾼다.
            // 검의 정식 세트 에셋은 무기 쪽에서 만든다.
            animationSet = new AnimatorOverrideController(animator.runtimeAnimatorController);
            animationSet[idleClip] = swordIdleClip;
            animationSet[attackPlaceholderClip] = swordAttackClip;

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

        // AC-5
        [Test]
        public void PlayAttackAnimation_PlaysAttackClip()
        {
            characterAnimation.ApplyAnimationSet(animationSet);
            int attackLayer = animator.GetLayerIndex(AttackLayer);
            Evaluate(IdleSpeed);
            Assert.That(PlayingClips(attackLayer), Is.Empty, "요청하기 전에는 공격 애니메이션이 재생되면 안 된다.");

            characterAnimation.PlayAttackAnimation();
            Evaluate(IdleSpeed);

            Assert.That(PlayingClips(attackLayer), Is.EquivalentTo(new[] { swordAttackClip }));
        }

        // AC-6: 세트가 상체를 덮고 있고 로코모션이 하체를 움직이는 중에도, 전신이 공격 애니메이션만 따라야 한다.
        [Test]
        public void PlayAttackAnimation_WhenSetAppliedAndMoving_PlaysAttackClipOnWholeBody()
        {
            characterAnimation.ApplyAnimationSet(animationSet);
            int attackLayer = animator.GetLayerIndex(AttackLayer);
            Evaluate(RunSpeed);
            Pose beforeAttack = CapturePose();

            // 대조: 요청하기 전에는 로코모션 속도에 따라 하체 포즈가 달라진다. 아래 비교가 의미를 가지려면 이 차이가 있어야 한다.
            animator.SetFloat(SpeedParameter, IdleSpeed);
            animator.Update(0f);
            Assert.That(MaxAngle(beforeAttack.Lower, CapturePose().Lower), Is.GreaterThan(1f));

            animator.SetFloat(SpeedParameter, RunSpeed);
            characterAnimation.PlayAttackAnimation();
            animator.Update(Step);
            Pose attackWhileRunning = CapturePose();
            Assert.That(PlayingClips(attackLayer), Is.EquivalentTo(new[] { swordAttackClip }));
            Assert.That(animator.GetLayerWeight(attackLayer), Is.EqualTo(1f));

            // 같은 시각에 로코모션만 바꿔도 포즈가 같아야 한다. 하체가 로코모션을 따르고 있다면 달라진다.
            animator.SetFloat(SpeedParameter, IdleSpeed);
            animator.Update(0f);
            AssertSamePose(attackWhileRunning, CapturePose(), "하체가 로코모션의 영향을 받았다.");

            // 상체를 덮는 세트 레이어를 꺼도 포즈가 같아야 한다.
            animator.SetLayerWeight(animator.GetLayerIndex(SwordArmLayer), 0f);
            animator.SetLayerWeight(animator.GetLayerIndex(SwordHandLayer), 0f);
            animator.Update(0f);
            AssertSamePose(attackWhileRunning, CapturePose(), "상체가 세트의 영향을 받았다.");

            // 공격 애니메이션이 실제로 상체와 하체 모두를 바꿨다.
            Assert.That(MaxAngle(beforeAttack.Upper, attackWhileRunning.Upper), Is.GreaterThan(1f), "상체가 바뀌지 않았다.");
            Assert.That(MaxAngle(beforeAttack.Lower, attackWhileRunning.Lower), Is.GreaterThan(1f), "하체가 바뀌지 않았다.");
        }

        // AC-7
        [Test]
        public void PlayAttackAnimation_AfterFinished_RestoresSetOnUpperBodyAndLocomotionOnLowerBody()
        {
            characterAnimation.ApplyAnimationSet(animationSet);
            int attackLayer = animator.GetLayerIndex(AttackLayer);
            Evaluate(RunSpeed);
            characterAnimation.PlayAttackAnimation();
            Evaluate(RunSpeed);
            Assert.That(PlayingClips(attackLayer), Is.EquivalentTo(new[] { swordAttackClip }), "공격이 시작되지 않았다.");

            // 휴머노이드 레이어는 빈 상태여도 켜져 있으면 아래 레이어를 기본 포즈로 덮어쓴다.
            // 그래서 공격을 재생하지 않는 프레임에는 레이어가 꺼져 있어야 하고, 이는 종료 프레임도 마찬가지다.
            bool finished = false;
            for (float elapsed = 0f; elapsed < swordAttackClip.length + Step * 2f; elapsed += Step)
            {
                animator.Update(Step);

                bool playing = PlayingClips(attackLayer).Length > 0;
                finished |= !playing;
                Assert.That(animator.GetLayerWeight(attackLayer), Is.EqualTo(playing ? 1f : 0f),
                    $"{elapsed + Step:0.0}초: 재생 여부와 공격 레이어의 가중치가 어긋났다.");
            }

            Assert.That(finished, Is.True, "공격 애니메이션이 끝나지 않았다.");
            Assert.That(PlayingClipNames(0), Is.EquivalentTo(new[] { "A_Run_F_Masc" }));
            Assert.That(PlayingClips(animator.GetLayerIndex(SwordArmLayer)), Is.EquivalentTo(new[] { swordIdleClip }));
            Assert.That(PlayingClips(animator.GetLayerIndex(SwordHandLayer)), Is.EquivalentTo(new[] { swordIdleClip }));

            // 반복 재생되지 않는다.
            Advance(swordAttackClip.length * 2f);

            Assert.That(PlayingClips(attackLayer), Is.Empty);
            Assert.That(animator.GetLayerWeight(attackLayer), Is.EqualTo(0f));
        }

        // AC-8
        [Test]
        public void PlayAttackAnimation_WhilePlaying_RestartsFromBeginning()
        {
            // 시험의 전제: 재요청 전에 절반쯤 재생되고, 재요청 뒤 절반을 더 재생해도 처음 요청 기준으로는 끝났을 길이여야 한다.
            float length = swordAttackClip.length;
            Assert.That(length, Is.GreaterThan(0.6f), "공격 클립이 너무 짧아 시험이 성립하지 않는다.");
            characterAnimation.ApplyAnimationSet(animationSet);
            int attackLayer = animator.GetLayerIndex(AttackLayer);
            Evaluate(IdleSpeed);
            characterAnimation.PlayAttackAnimation();
            Advance(length * 0.5f);
            float beforeRestart = animator.GetCurrentAnimatorStateInfo(attackLayer).normalizedTime;
            Assert.That(beforeRestart, Is.GreaterThan(0.2f / length), "공격이 재생되지 않았다.");

            characterAnimation.PlayAttackAnimation();
            animator.Update(Step);

            float afterRestart = animator.GetCurrentAnimatorStateInfo(attackLayer).normalizedTime;
            Assert.That(afterRestart, Is.LessThan(beforeRestart));
            Assert.That(afterRestart, Is.LessThan(Step * 1.5f / length), "재생 위치가 처음으로 돌아가지 않았다.");
            Assert.That(animator.GetLayerWeight(attackLayer), Is.EqualTo(1f), "다시 시작하면서 공격 레이어가 꺼졌다.");

            // 다시 시작한 프레임의 포즈가 같은 시각의 재평가와 같아야 한다. 레이어가 늦게 켜지거나 잠깐 꺼지면 달라진다.
            Pose atRestart = CapturePose();
            animator.Update(0f);
            AssertSamePose(atRestart, CapturePose(), "다시 시작한 프레임에 공격 포즈가 아니었다.");

            // 처음 요청을 기준으로는 이미 끝났을 시각이지만, 다시 시작했으므로 아직 재생 중이어야 한다.
            Advance(length * 0.5f);

            Assert.That(PlayingClips(attackLayer), Is.EquivalentTo(new[] { swordAttackClip }));
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
        public void Controller_HasAttackLayerOverWholeBodyAboveArmLayers()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            var layerNames = controller.layers.Select(layer => layer.name).ToList();
            AnimatorControllerLayer attack = controller.layers.Single(layer => layer.name == AttackLayer);

            Assert.That(layerNames.IndexOf(AttackLayer), Is.GreaterThan(layerNames.IndexOf(SwordArmLayer)));
            Assert.That(layerNames.IndexOf(AttackLayer), Is.GreaterThan(layerNames.IndexOf(SwordHandLayer)));
            Assert.That(attack.blendingMode, Is.EqualTo(AnimatorLayerBlendingMode.Override));
            Assert.That(attack.avatarMask, Is.Null, "공격 레이어에 마스크가 있으면 전신을 덮지 못한다.");
            // 쉬는 동안에는 꺼져 있어야 한다. 빈 상태여도 켜져 있으면 아래 레이어를 기본 포즈로 덮어쓴다.
            Assert.That(attack.defaultWeight, Is.EqualTo(0f));

            // 재생하는 동안 레이어를 켜는 것은 공격 상태가 맡는다.
            AnimatorState playing = attack.stateMachine.states
                .Select(child => child.state)
                .Single(state => state.motion == attackPlaceholderClip);
            Assert.That(playing.behaviours.OfType<EnableLayerWhilePlaying>().Count(), Is.EqualTo(1));
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

        private void Advance(float seconds)
        {
            for (float elapsed = 0f; elapsed < seconds; elapsed += Step)
            {
                animator.Update(Step);
            }
        }

        private readonly struct Pose
        {
            public readonly Quaternion[] Upper;
            public readonly Quaternion[] Lower;

            public Pose(Quaternion[] upper, Quaternion[] lower)
            {
                Upper = upper;
                Lower = lower;
            }
        }

        private Pose CapturePose()
        {
            return new Pose(Rotations(UpperBodyBones), Rotations(LowerBodyBones));
        }

        private static float MaxAngle(Quaternion[] a, Quaternion[] b)
        {
            return a.Select((rotation, i) => Quaternion.Angle(rotation, b[i])).Max();
        }

        private static void AssertSamePose(Pose expected, Pose actual, string message)
        {
            Assert.That(MaxAngle(expected.Upper, actual.Upper), Is.LessThan(0.01f), $"{message} (상체)");
            Assert.That(MaxAngle(expected.Lower, actual.Lower), Is.LessThan(0.01f), $"{message} (하체)");
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
