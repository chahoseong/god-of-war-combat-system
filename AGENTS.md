# God of War Combat System — Agent Guide

God of War-style third-person melee combat, built as an implementation portfolio. Unity 6, URP, Input System, Cinemachine (exact versions: `ProjectSettings/ProjectVersion.txt`, `Packages/manifest.json`).
Comments, SPECs, commit messages, and assertion messages are written in Korean; identifiers stay in English.

## 1. Sources of truth
- Precedence (higher wins): §3 of this file > SPEC AC > `Docs/GDD.md` > `Docs/Research/`. If a request conflicts with a higher layer, stop and ask.
- `Docs/GDD.md`: combat vision, attack lifecycle (Windup / Active / Recovery), HitStop, control authority matrix. Read it before changing combat rules.
- Module layout: one module name maps to
  - `Specs/<Module>/SPEC.md`
  - `Assets/Scripts/<logic root>/<Module>/` and `Assets/Scripts/<adapter root>/<Module>/` (see §3)
  - `Assets/Tests/EditMode/<Module>/`
  Read the module's SPEC before touching its code. Code in a folder with no matching SPEC is unspecified: fix bugs only when asked; no new behavior without a SPEC.
- Transition: the assembly split (§3) and module layout are being introduced. `Specs/Character/`, `Specs/Weapon/`, and `Assets/Scripts/{Character,Weapons,Player}/` predate them and are being replaced. Do not extend them.
- SPEC format: Overview → `FR-n` → `AC-n` (one observable outcome each, ending with `(FR-n)`) → Out of Scope. An AC may be marked `(manual)` only when the user approves it with the SPEC.
- When an experiment confirms engine or API behavior, record it as `Docs/Research/YYYY-MM-DD-topic.md`.

## 2. Workflow and approval gates
1. Spec: draft new or changed FR/AC, then stop for approval. Never change an existing AC's meaning without approval.
2. Brief: before creating or editing files, present the files to change, Editor operations, candidate names for new types, assemblies, and folders (the user picks names), and an AC → test name table (no test code). Wait for an explicit go. An answer to one question in the brief is not a go.
3. Red: write the tests from the table. Red means the tests compile and fail on an assertion; add minimal stubs first so the test assembly compiles.
4. Green: write only the code the tests require. No public API, parameters, or features that no test requires. Adapter wiring that only forwards Unity callbacks to tested logic needs no test of its own.
5. Validate: pass every gate in §4, then report.

After the go, proceed through Red → Green → Validate without stopping, except for the cases in §5.

Non-feature work (refactors such as moving code between assemblies, test or tooling infrastructure, docs, bug fixes in unspecified code): no new FR/AC; behavior must not change; all existing tests stay green. Still brief and wait for a go.

## 3. Architecture and engine invariants
Apply to `Assets/Scripts/` and `Assets/Tests/`.
- Humble Object:
  - Combat rules (phases, frame windows, control authority, input buffer, HitStop timers, damage, knockback) live in the logic assembly. It sets `noEngineReferences: true`, so it cannot use `UnityEngine` (`Time`, `Random`, `Debug`, `MonoBehaviour`).
  - Time is injected (`Tick(float dt)`). Deterministic = same inputs + same `dt` sequence ⇒ same state.
  - The adapter assembly holds MonoBehaviours and ScriptableObjects. They only translate between Unity and the logic: read input and `Time.deltaTime`, call `Tick`, apply results (CharacterController, Animator, physics queries). Only the adapter assembly references Input System.
  - The test assembly references both. No assembly references the test assembly. `UnityEditor` only inside `#if UNITY_EDITOR`.
- Zero-GC in hot paths:
  - Hot path = `Update`/`FixedUpdate`/`LateUpdate`, `OnTrigger*`/`OnCollision*`, Animation Events, Input System callbacks, `StateMachineBehaviour.OnState*`, `OnAnimatorMove`/`OnAnimatorIK`, coroutine bodies, and anything they call. Allocation is fine in `Awake`, `Start`, `OnEnable`/`OnDisable`, `OnValidate`, and tests.
  - In hot paths: cache components and `Camera.main` in `Awake`; cache Animator parameters with `static readonly` `Animator.StringToHash`; no LINQ, string building, `.name`, `Debug.Log*`, per-call delegate creation, array-returning physics queries (use preallocated buffers), or `Instantiate`/`Destroy` for hit VFX/SFX (use `UnityEngine.Pool.ObjectPool<T>`).
  - Each logic `Tick` has a test: `Assert.That(() => sut.Tick(dt), Is.Not.AllocatingGCMemory());` with `using Is = UnityEngine.TestTools.Constraints.Is;`.
- Fake null: a destroyed `UnityEngine.Object` is non-null to C#; only Unity's `==`/`!=`/`bool` overloads, chosen by the compile-time type, detect it.
  - On Unity objects use `obj == null`, `obj != null`, `if (obj)`. Never `is null`, `is not null`, `?.`, `??`, `??=`, `ReferenceEquals`, or pattern matching.
  - Through an interface or generic, `t == null` misses destroyed objects: check `t is UnityEngine.Object o && o != null`, or constrain `where T : UnityEngine.Object`.
- Serialization: `[SerializeField] private Type _fieldName;`. Expose read-only properties and collections as `IReadOnlyList<T>`. No public mutable fields. When renaming a serialized field, add `[FormerlySerializedAs("_oldName")]`; otherwise scene and prefab data is silently lost.
- Domain Reload is disabled (Enter Play Mode Options): statics and static event subscriptions survive Play sessions. No mutable statics, `static Instance` singletons, `DontDestroyOnLoad` managers, or `Find*`/`FindObjectsByType` lookups; pass dependencies via `[SerializeField]` or constructor/method parameters. If a mutable static is unavoidable, reset it in `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`.
- Unity 6 APIs only (Input System, never `UnityEngine.Input`; `Unity.Cinemachine`). Obsolete-API warnings fail §4. If unsure an API exists, check `Library/PackageCache/` or reflect through the Editor; do not guess.
- `MonoBehaviour`/`ScriptableObject` class name equals its file name. No `async void`; no `Task` in hot paths; use `Awaitable` if async is required.

## 4. Definition of done
1. Scope: the change traces to an approved FR/AC, or is non-feature work per §2. Nothing under a SPEC's Out of Scope is implemented, even partially.
2. Tests:
   - Every automated AC has at least one EditMode test tagged `[Test, Property("AC", "<Module>#AC-n")]`. Manual ACs get a Play mode check procedure in the report instead.
   - Every test carries an AC tag, except Zero-GC tests, which are tagged `[Property("Invariant", "ZeroGC")]`. Do not add tests that no AC requires.
   - Name tests `<Condition>_<ExpectedResult>` so the name reads as the AC.
   - EditMode does not run the Player Loop, physics, or Animator, and does not call `Awake`/`OnEnable`. Drive them explicitly (`animator.Update(dt)`, `Physics.Simulate(dt)`) or test the logic directly. Never move caching into hot paths to work around a missing `Awake`. Do not create PlayMode assemblies without approval.
   - Test integrity: never delete, `[Ignore]`, loosen, or change the meaning of an existing assertion to make it pass. Setup or API changes that preserve each assertion's meaning are allowed; list rewritten tests in the report. Tests of an AC retired with user approval are deleted with it.
3. Run (the Editor is normally open; use the Unity CLI):
   - `unity status` is `ready` → `unity command recompile`, then poll `unity command recompile_status` → `unity command console_status` shows no compile failure and `unity command console --level warn` shows no `error CS`/`warning CS` from `CombatSystem*` assemblies → `unity command run_tests --mode editor` passes.
   - No Editor running: `unity test . --mode EditMode`.
   - If you could not run the tests, write `UNVERIFIED`. Never claim tests pass from reading code.
4. Blast radius:
   - Edit only the files declared in the brief; explain any other file you touch. Do not reformat, rename, or reorder lines you did not need to change.
   - Scenes, prefabs, controllers, clips, and other assets: change them only through the live Editor (`unity command`, `eval_file`), never by editing Unity YAML. Declare them in the brief.
   - `.meta`: never write one by hand. After creating a file, let the Editor import it to generate the `.meta`. Move or rename assets with `move_asset`/`rename_asset`. Commit each asset with its `.meta`.
   - Ask before touching: `ProjectSettings/**`, `Packages/**`, `Assets/ThirdParty/**`, `*.asmdef`, `*.inputactions` and generated input classes, `Docs/GDD.md`.
   - Commit, push, or branch only when asked.
5. Report: AC → test table with pass / fail / `UNVERIFIED` / manual per AC; files changed; open questions.

## 5. Stop and ask
After the go in §2, stop only when:
- A new or changed FR/AC is needed, or an existing test or AC looks wrong.
- The work conflicts with §3, a SPEC, or the GDD.
- An ask-before path (§4.4) must change.
- Something outside the brief must change.
