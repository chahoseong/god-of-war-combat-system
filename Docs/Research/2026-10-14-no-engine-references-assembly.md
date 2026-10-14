# noEngineReferences 로직 어셈블리 실험

- 날짜: 2026-10-14
- 환경: Unity 6000.6.4f1, API 호환성 수준 .NET Standard 2.1 (`apiCompatibilityLevel: 6`), com.unity.test-framework (PackageCache `@80f691d4262b`)
- 대상: `Assets/Scripts/Core/CombatSystem.Core.asmdef` (`noEngineReferences: true`, 참조 없음)
- 방법: 로직 어셈블리에 임시 클래스, 테스트 어셈블리에 임시 테스트를 두고 Unity CLI(`recompile`, `console_status`, `run_tests --mode editor`)로 확인한 뒤 임시 파일은 삭제했다.

## 결과

| 질문 | 결과 |
|---|---|
| `noEngineReferences: true` 어셈블리에서 `System.Numerics.Vector2`를 쓰는 코드가 컴파일되는가 | 된다. `CombatSystem.Core.dll` 생성, 오류·경고 0 |
| 그 어셈블리에서 `UnityEngine`을 쓰면 컴파일 오류가 나는가 | 난다. `error CS0103: The name 'UnityEngine' does not exist in the current context` |
| 테스트 어셈블리가 참조해서 EditMode 테스트가 실행되는가 | 된다. 테스트 asmdef `references`에 `"CombatSystem.Core"` 추가 |
| `System.Numerics.Vector2` 연산이 `Is.Not.AllocatingGCMemory()`를 통과하는가 | 통과한다. 같은 환경에서 `new object()`는 `Is.AllocatingGCMemory()`로 잡히는 것을 함께 확인했다 |

## 함께 확인한 것

- `AllocatingGCMemory`는 `UnityEngine.TestTools.Constraints` 네임스페이스의 확장 메서드다. `using Is = UnityEngine.TestTools.Constraints.Is;` 별칭만으로는 `Is.Not.AllocatingGCMemory()`가 컴파일되지 않는다(`error CS1061`). `using UnityEngine.TestTools.Constraints;`가 함께 있어야 한다.
- `Assert.That(() => sut.Tick(dt), Is.Not.AllocatingGCMemory())`에서 `Tick`이 값을 돌려주면 람다가 값 반환 대리자로 해석되어, 실행 결과 값이 제약에 넘어가 `ArgumentException: The actual value must be a TestDelegate`로 실패한다. 블록 람다 `() => { sut.Tick(dt); }`로 써야 한다.
- 스크립트가 하나도 없는 asmdef는 컴파일할 때마다 경고를 낸다: `Assembly for Assembly Definition File '...' will not be compiled, because it has no scripts associated with it.` 테스트 어셈블리의 참조와는 무관하고, 첫 스크립트가 생기면 사라진다.
