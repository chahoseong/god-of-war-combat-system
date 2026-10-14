# God of War Combat System — Agent Constitution

---

## 1. Identity & Architecture
- **Purpose**: God of War-style 3rd-person melee combat portfolio. Unity 6 + URP + Input System + Cinemachine.
  Exact versions: `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json` (SSOT).
- **Core Philosophy**: Spec-Driven Development (SDD) & Acceptance Criteria (AC)-based Test-Driven Development (TDD).
  - *SDD Cycle* (each step is a gate; do not skip):
    1. Spec: Draft new/changed FR/AC in the module's `SPEC.md`, then stop for approval (§5). Never change an existing AC's meaning without approval.
    2. Test (Red): Write EditMode tests for each AC and confirm they **fail** for the expected reason.
    3. Production (Green): Write only the code needed to pass those tests. No public API, parameters, or features that no test requires.
    4. Validation: Pass every gate in §4.
- **Assembly Boundaries & Dependency Rule**:
  - `CombatSystem.asmdef` (`Assets/Scripts/`, namespace `CombatSystem`): Production runtime logic. Must not reference test assemblies or `UnityEditor`, except inside `#if UNITY_EDITOR` blocks (e.g., `OnValidate`, `OnDrawGizmos`).
  - `CombatSystem.Tests.EditMode.asmdef` (`Assets/Tests/EditMode/`, namespace `CombatSystem.Tests.EditMode`): NUnit test suite. Strict one-way dependency (`Tests` -> `CombatSystem`).
  - Before changing any `.asmdef` (e.g., adding `Unity.Cinemachine`), state the reason in your response.

---

## 2. Pointers to SSOT (Load on Demand)
- **Precedence** (higher wins on conflict): §3 of this file > SPEC.md AC > `Docs/GDD.md` > `Docs/Research/`. If a user request conflicts with a higher layer, stop and ask (§5).
- **Combat Vision & GDD**: [`Docs/GDD.md`](Docs/GDD.md)
  - Consult when tuning mechanics, frame timing (Windup, Active, Recovery), HitStop durations, input buffering, and camera shakes.
- **Module Specs**: Read the module's SPEC before touching its script. Authoritative for requirements and scope.

  | Module | Spec | Production | Tests |
  |---|---|---|---|
  | Animation | [`Specs/Character/Animation/SPEC.md`](Specs/Character/Animation/SPEC.md) | `CharacterAnimation.cs` | `CharacterAnimationTests.cs` |
  | Attack | [`Specs/Character/Attack/SPEC.md`](Specs/Character/Attack/SPEC.md) | `CharacterAttack.cs` | `CharacterAttackTests.cs` |
  | Equipment | [`Specs/Character/Equipment/SPEC.md`](Specs/Character/Equipment/SPEC.md) | `CharacterEquipment.cs` | `CharacterEquipmentTests.cs` |
  | Weapon | [`Specs/Weapon/SPEC.md`](Specs/Weapon/SPEC.md) | `Weapon.cs` | `WeaponTests.cs` |

  - *Unspecified code* (`CharacterMovement.cs`, `PlayerController.cs`, `EnableLayerWhilePlaying.cs`): fix bugs only when asked. No new behavior without a SPEC first.
  - *SPEC format*: Overview → `FR-n` → `AC-n` (one observable outcome each, ending with `(FR-n)`) → Out of Scope.
- **Conventions (detail)**: [`Docs/Conventions/HotPath.md`](Docs/Conventions/HotPath.md) (full zero-GC list), [`Docs/Conventions/Unity6API.md`](Docs/Conventions/Unity6API.md) (renamed/obsolete APIs). Consult when writing hot-path code or using an unfamiliar Unity API.
- **Technical Research & Spikes**: [`Docs/Research/`](Docs/Research/)
  - Consult for prior evaluations. When you confirm engine/API behavior by experiment, record it as `Docs/Research/YYYY-MM-DD-topic.md`.

---

## 3. Engine & Performance Constraints (Hard Invariants)
Apply to `Assets/Scripts/` and `Assets/Tests/` only. They exist to avoid GC spikes and engine-lifecycle bugs during combat.
- **Zero-GC in Hot Paths**:
  - *Hot path* = any code that can run during combat frames: `Update`/`FixedUpdate`/`LateUpdate`, `OnTrigger*`/`OnCollision*`, Animation Events, Input System callbacks, `StateMachineBehaviour.OnState*`, `OnAnimatorMove`/`OnAnimatorIK`, coroutine bodies, and anything they call. Allocation is allowed in `Awake`, `Start`, `OnEnable`/`OnDisable`, `OnValidate`, and tests.
  - *Banned in hot paths* (full list: `Docs/Conventions/HotPath.md`):
    - `GetComponent*`/`TryGetComponent`, `Camera.main`, `Find*` → cache in `Awake`.
    - LINQ; lambdas or method groups converted to delegates.
    - String building (`$"..."`, `+`, `string.Format`), `.name`, `.tag ==` (use `CompareTag`), `Debug.Log*`.
    - Array-returning queries (`Physics.OverlapSphere`, `Physics.RaycastAll`, ...) → buffer overloads (e.g., `Physics.OverlapSphereNonAlloc`) with preallocated arrays.
    - `Instantiate`/`Destroy` for hit VFX/SFX → `UnityEngine.Pool.ObjectPool<T>`.
  - *Enforced Patterns*: Cache component references in `Awake()`. Cache Animator parameters via static readonly `Animator.StringToHash()`.
  - *Verification*: Each hot-path method of core logic has a test: `Assert.That(() => sut.Tick(dt), Is.Not.AllocatingGCMemory());` (`using Is = UnityEngine.TestTools.Constraints.Is;`).
- **Determinism & Testability (Humble Object)**:
  - Combat rules (frame windows, combo/buffer, HitStop timers, damage) live in plain C# classes. MonoBehaviours only adapt Unity callbacks to them.
  - Time is injected (`Tick(float dt)`). Core logic must not read `Time.*`, unseeded `UnityEngine.Random`, or `DateTime.Now`.
  - Deterministic = same input sequence + same `dt` sequence ⇒ same state.
- **Unity Lifecycle Semantics (Fake Null)**:
  - *Rationale*: A destroyed `UnityEngine.Object` is non-null in C# but dead in the engine. Only Unity's overloaded `==`/`!=`/`bool` detect this, and C# picks the overload by the variable's **compile-time type**.
  - *DO* (variable typed as `UnityEngine.Object` or a subclass): `obj == null`, `obj != null`, `if (obj)`, `if (!obj)`.
  - *DO NOT* on Unity objects: `is null`, `is not null`, `?.`, `??`, `??=`, `ReferenceEquals`, pattern matching (`obj is Weapon w`).
  - *TRAP (interfaces/generics)*: `IDamageable t; t == null` uses C# reference equality and **misses destroyed objects**. Check via the concrete type: `t is UnityEngine.Object o && o != null`. Generics handling Unity objects must use `where T : UnityEngine.Object`.
- **Strict Encapsulation & Serialization**:
  - *DO*: Declare serialized fields as `[SerializeField] private Type _fieldName;`.
  - *DO*: Expose read-only properties (`public Type FieldName => _fieldName;`) when external systems require observation. Expose collections as `IReadOnlyList<T>`, never the backing `List<T>`/array.
  - *DO*: When renaming a serialized field, add `[FormerlySerializedAs("_oldName")]`. Without it, scene/prefab data is silently lost.
  - *DO NOT*: Declare public mutable fields for serialization or state exposure.
- **Engine Environment**:
  - **Domain Reload is disabled** (Enter Play Mode Options). Statics and static event subscriptions survive across Play sessions. Do not use mutable statics. If one is unavoidable, reset it in `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.
  - `MonoBehaviour`/`ScriptableObject` class name must equal its file name. When moving or renaming a script, move its `.meta` with it.
  - Use Unity 6 APIs only (e.g., `Rigidbody.linearVelocity`, `Unity.Cinemachine`, Input System; never `UnityEngine.Input`). If unsure an API exists, check `Library/PackageCache/` or `Docs/Conventions/Unity6API.md`. Do not guess.
  - No `async void`. No `Task` in hot paths. Use `Awaitable` if async is required.

---

## 4. Verification & Definition of Done (DoD)
A task is complete **only** when all of the following gates pass:
1. **Spec Alignment**: The change traces to an approved FR/AC. Items under a SPEC's **Out of Scope** must not be implemented, even partially.
2. **Test Verification**:
   - Every AC has at least one EditMode test that declares it: `[Test, Property("AC", "Character/Attack#AC-1")]` (spec folder under `Specs/` + `#` + AC ID).
   - Run: `& "C:\Program Files\Unity\Hub\Editor\6000.6.4f1\Editor\Unity.exe" -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults Logs/editmode-results.xml -logFile Logs/editmode.log` (exit 0 = pass, 2 = failures, other = run error). If an open Editor locks the project, ask the user to run the Test Runner and share the result.
   - 0 compiler errors/warnings in `CombatSystem*` assemblies. Check `Logs/editmode.log` or `%LOCALAPPDATA%\Unity\Editor\Editor.log` for `error CS` / `warning CS`.
   - **If you could not run the tests, write `UNVERIFIED` in your final response. Never claim tests pass based on reading code.**
   - EditMode does not run the Player Loop, physics, or Animator automatically. Drive them in tests (`animator.Update(dt)`, `Physics.Simulate(dt)`) or move logic into a Humble Object (§3). Do not create PlayMode assemblies without approval.
   - **Test integrity**: Never delete, `[Ignore]`, loosen, or rewrite an existing assertion to make it pass.
3. **Minimal Blast Radius**:
   - Before editing, list the files you plan to change. Explain any file you touch outside that list.
   - Protected (edit only on explicit request): `ProjectSettings/**`, `Packages/**`, `Assets/ThirdParty/**`, `*.asmdef`, `*.meta`, `*.unity`, `*.prefab`, `*.asset`, `*.controller`, `*.anim`, `*.inputactions` and generated input classes, `Docs/GDD.md`. Never hand-edit Unity YAML or create/delete/regenerate `.meta` files.
   - Do not format, rename, reorder, or clean up lines you did not need to change.
   - Do not commit, push, or create branches unless asked.
4. **Anti-Goals Alignment**:
   - Banned: mutable `static` fields, `static ... Instance` singletons, `DontDestroyOnLoad` managers, `Find*`/`FindObjectsByType` for dependency lookup. Pass dependencies via `[SerializeField]` or constructor/method parameters.
   - No feature bloat: exclude complex RPG progression, inventory trees, or networking.
5. **Completion Report** (required in the final response):
   - AC → test table with pass / fail / `UNVERIFIED` per AC.
   - Files changed (should match the declared list).
   - Open questions or ACs that could not be verified in EditMode.

---

## 5. Stop & Ask
Stop and ask the user before proceeding when:
- A new or changed FR/AC is needed.
- An existing test or AC looks wrong.
- An AC cannot be verified in EditMode.
- The request conflicts with §3 or a SPEC.
- A protected path (§4.3) must change.
