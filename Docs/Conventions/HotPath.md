# Hot Path Zero-GC Conventions

Detail for `AGENTS.md` §3. "Hot path" is defined there.

| Banned in hot paths | Use instead |
|---|---|
| `GetComponent*`, `TryGetComponent`, `GetComponents` (array overload), `Camera.main`, `Find*` | Cache in `Awake`; for hit targets, cache a `Collider → component` map or use buffer overloads |
| LINQ | `for` loops over concrete collections |
| Lambdas / method groups converted to delegates (`Action a = OnHit;`) | Create the delegate once in `Awake` and reuse it |
| `params` calls | Fixed-arity overloads |
| `$"..."`, `+`, `string.Format`, `string.Concat` | Precomputed strings; no strings in hot paths |
| `.name`, `.tag == "X"` | Cached values; `CompareTag("X")` |
| `Debug.Log*` | `[System.Diagnostics.Conditional]` wrapper, or none |
| `Enum.ToString()`, `Enum.HasFlag` | Cached strings; bitwise `(flags & X) != 0` |
| `foreach` over interface types (`IEnumerable<T>`, `IList<T>`) | `for`, or `foreach` over concrete `List<T>`/array |
| struct → interface casts; struct dictionary keys without `IEquatable<T>` | Generic constraints; implement `IEquatable<T>` |
| `Physics.OverlapSphere`, `Physics.RaycastAll`, other array-returning queries | Buffer overloads (e.g., `Physics.OverlapSphereNonAlloc`) with preallocated arrays |
| `Animator.GetCurrentAnimatorClipInfo()` (array) | `List<AnimatorClipInfo>` overload |
| `new WaitForSeconds(...)`, `StartCoroutine` per hit | Cached yield instructions, or timer structs ticked by `dt` |
| `Instantiate` / `Destroy` for VFX/SFX | `UnityEngine.Pool.ObjectPool<T>` |
| `Task`, `async void` | `Awaitable` (outside hot paths) |

## Verification

```csharp
using Is = UnityEngine.TestTools.Constraints.Is;

Assert.That(() => sut.Tick(dt), Is.Not.AllocatingGCMemory());
```

> Note: caching `.transform` is a micro-optimization, not a GC rule.
