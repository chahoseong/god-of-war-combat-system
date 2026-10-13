# God of War Combat System — Agent Constitution

---

## 1. Identity & Architecture
- **Purpose**: God of War style 3rd-person melee combat system portfolio built on Unity 6 (`6000.6.4f1`), URP 17.6, New Input System 1.20, and Cinemachine 6.6.
- **Core Philosophy**: Spec-Driven Development (SDD) & Acceptance Criteria (AC)-based Test-Driven Development (TDD).
  - *SDD Cycle*:
    1. Spec: Define Functional Requirements (FR) and Acceptance Criteria (AC) in `Specs/**/SPEC.md`.
    2. Test: Implement automated NUnit tests under `Assets/Tests/EditMode/` mapping to each AC.
    3. Production: Implement minimal, high-cohesion logic in `Assets/Scripts/` to pass tests.
    4. Validation: Verify all EditMode tests pass with zero regressions.
- **Assembly Boundaries & Dependency Rule**:
  - `CombatSystem.asmdef` (`Assets/Scripts/`, namespace `CombatSystem`): Production runtime logic. Prohibited from referencing test assemblies or editor APIs.
  - `CombatSystem.Tests.EditMode.asmdef` (`Assets/Tests/EditMode/`, namespace `CombatSystem.Tests.EditMode`): NUnit test suite. Strict one-way dependency (`Tests` -> `CombatSystem`).
  - *Rationale*: Enforces clean domain boundaries and ensures test runners or editor-only dependencies never pollute standalone runtime builds.

---

## 2. Pointers to SSOT (Load on Demand)
Conserve attention budget by loading documentation on-demand instead of dumping into prompt context:
- **Combat Vision & GDD**: [`Docs/GDD.md`](Docs/GDD.md)
  - Consult when tuning mechanics, frame timing (Windup, Active, Recovery), HitStop durations, input buffering, and camera shakes.
  - Core Pillars: Weight & Impact, Commitment & Fluidity, Clarity & Feedback.
- **Module Specs & Acceptance Criteria**: [`Specs/**/SPEC.md`](Specs/)
  - Consult before implementing or modifying any module. Authoritative contracts for requirements and scope boundaries.
  - Key Modules: `Specs/Character/Animation/`, `Specs/Character/Attack/`, `Specs/Character/Equipment/`, `Specs/Weapon/`.
- **Technical Research & Spikes**: [`Docs/Research/`](Docs/Research/)
  - Consult or record architectural evaluations, benchmark findings, and engine spikes.

---

## 3. Engine & Performance Constraints (Hard Invariants)
Strict technical invariants enforced across all runtime code to guarantee locked 60 FPS combat:
- **Zero-GC in Hot Paths**:
  - *Rationale*: To prevent garbage collection frame drops and GC stalls during combat, heap allocations are strictly banned in per-frame loops (`Update`, `FixedUpdate`, `LateUpdate`).
  - *Banned in hot paths*:
    - `GetComponent<T>()` and `Camera.main` (perform cache in `Awake`).
    - LINQ expressions (`.Select()`, `.Where()`, etc.) and heap-allocating closures/lambdas.
    - String formatting and concatenation (`$"..."`, `string.Concat`, `+`).
    - Allocating physics queries: use `Physics.OverlapSphereNonAlloc` with preallocated buffers instead of `Physics.OverlapSphere`.
  - *Enforced Patterns*: Cache component references in `Awake()`. Cache Animator parameters via static readonly `Animator.StringToHash()`.
- **Unity Lifecycle Semantics (Fake Null)**:
  - *Rationale*: `UnityEngine.Object` wraps a native C++ engine object. Unity overrides `operator ==` and `operator !=` to check whether the underlying C++ object is alive. C# direct operators check only the managed CLR wrapper and fail to detect destroyed objects ("fake null"), causing silent bugs and memory leaks.
  - *DO*: `obj == null`, `obj != null`, `if (obj)`, `if (!obj)`.
  - *DO NOT*: `obj is null`, `obj is not null`, `obj?.Property`, `obj ?? fallback`, `object.ReferenceEquals()`.
- **Strict Encapsulation & Serialization**:
  - *Rationale*: To maintain robust encapsulation and prevent unexpected external mutations while preserving Unity's Inspector workflow.
  - *DO*: Declare serialized fields as `[SerializeField] private Type _fieldName;`.
  - *DO*: Expose read-only properties (`public Type FieldName => _fieldName;`) when external systems require observation.
  - *DO NOT*: Declare public mutable fields for serialization or state exposure.

---

## 4. Verification & Definition of Done (DoD)
A task is considered complete **only** when all of the following deterministic gates pass:
1. **Spec Alignment**: Consult existing [`Specs/**/SPEC.md`](Specs/) prior to writing code. Author or update FR/AC before adding new features.
2. **Deterministic Test Verification**:
   - Every Acceptance Criterion (AC) must be validated with corresponding NUnit EditMode tests in `Assets/Tests/EditMode/`.
   - All tests must compile without errors/warnings and pass deterministically.
3. **Minimal Blast Radius**:
   - Strictly limit edits to files directly related to the user request.
   - Do not perform unsolicited code formatting, whitespace cleanup, or unrelated refactoring.
4. **Anti-Goals Alignment**:
   - No Singleton abuse or hidden global state: pass dependencies explicitly via serialized fields or injection.
   - No feature bloat: exclude complex RPG progression, inventory trees, or networking.
   - Focus exclusively on responsive, heavyweight, deterministic melee combat mechanics.
