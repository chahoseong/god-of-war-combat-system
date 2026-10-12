using System.Linq;
using CombatSystem.Character;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CombatSystem.Tests.EditMode
{
    public class CharacterAttackTests
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player.prefab";
        private const string AttackPlaceholderPath = "Assets/Animations/Character/AttackPlaceholder.anim";
        private const string AttackLayer = "Attack";

        // 한 번의 Update 로 진행시키는 시간. 길게 한 번에 진행시키면 상태 전환이 한 프레임에 몰린다.
        private const float Step = 0.1f;

        private GameObject player;
        private CharacterAttack characterAttack;
        private Animator animator;
        private AnimationClip attackPlaceholderClip;

        [SetUp]
        public void SetUp()
        {
            // EditMode 에서는 Awake/LateUpdate 가 불리지 않는다. Animator 를 직접 갱신한다.
            // 프리팹에는 아직 CharacterAttack 이 없으므로 인스턴스에 붙여 쓴다.
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            player = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            characterAttack = player.AddComponent<CharacterAttack>();

            CharacterAnimation characterAnimation = player.GetComponent<CharacterAnimation>();
            Assert.That(characterAnimation, Is.Not.Null, "Player 프리팹에 CharacterAnimation 이 없다.");

            SerializedObject serializedAnimation = new SerializedObject(characterAnimation);
            animator = (Animator)serializedAnimation.FindProperty("animator").objectReferenceValue;
            Assert.That(animator, Is.Not.Null, "CharacterAnimation 의 animator 가 연결되지 않았다.");

            // 전달만 보면 되므로 시험용 세트 없이 기본 컨트롤러의 자리표시 클립을 쓴다.
            attackPlaceholderClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(AttackPlaceholderPath);
            Assert.That(attackPlaceholderClip, Is.Not.Null, $"{AttackPlaceholderPath} 가 없다.");

            // EditMode 에는 보이는 렌더러가 없어서 기본 설정(Cull Update Transforms)에서는 클립 정보가 갱신되지 않는다.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(player);
        }

        // AC-1: 한 번 요청하면 재생이 시작되고, 추가 요청 없이는 되감기거나 다시 시작하지 않고 끝난다.
        [Test]
        public void Attack_PlaysAttackAnimationOnce()
        {
            int attackLayer = animator.GetLayerIndex(AttackLayer);
            Assert.That(attackLayer, Is.GreaterThanOrEqualTo(0), $"컨트롤러에 '{AttackLayer}' 레이어가 없다.");
            animator.Update(Step);
            Assert.That(PlayingClips(attackLayer), Is.Empty, "요청하기 전에는 공격 애니메이션이 재생되면 안 된다.");

            characterAttack.Attack();
            animator.Update(Step);

            Assert.That(PlayingClips(attackLayer), Is.EquivalentTo(new[] { attackPlaceholderClip }),
                "공격을 요청했는데 공격 애니메이션이 재생되지 않았다.");
            Assert.That(animator.GetLayerWeight(attackLayer), Is.EqualTo(1f));

            // 재생 위치가 되감기면 요청이 한 번 더 전달된 것이다.
            float previousTime = animator.GetCurrentAnimatorStateInfo(attackLayer).normalizedTime;
            bool finished = false;
            for (float elapsed = 0f; elapsed < attackPlaceholderClip.length + Step * 2f; elapsed += Step)
            {
                animator.Update(Step);
                if (PlayingClips(attackLayer).Length == 0)
                {
                    finished = true;
                    break;
                }

                float currentTime = animator.GetCurrentAnimatorStateInfo(attackLayer).normalizedTime;
                Assert.That(currentTime, Is.GreaterThanOrEqualTo(previousTime),
                    $"{elapsed + Step:0.0}초: 재생 위치가 되감겼다. 요청이 한 번 더 전달되었다.");
                previousTime = currentTime;
            }

            Assert.That(finished, Is.True, "공격 애니메이션이 끝나지 않았다.");

            // 끝난 뒤 시간이 더 지나도 다시 시작되지 않는다.
            for (float elapsed = 0f; elapsed < attackPlaceholderClip.length * 2f; elapsed += Step)
            {
                animator.Update(Step);
                Assert.That(PlayingClips(attackLayer), Is.Empty, $"끝난 뒤 {elapsed + Step:0.0}초: 공격 애니메이션이 다시 재생되었다.");
            }

            Assert.That(animator.GetLayerWeight(attackLayer), Is.EqualTo(0f));
        }

        private AnimationClip[] PlayingClips(int layer)
        {
            return animator.GetCurrentAnimatorClipInfo(layer)
                .Where(info => info.weight > 0f)
                .Select(info => info.clip)
                .ToArray();
        }
    }
}
