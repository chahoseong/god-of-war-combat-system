# Unity 6 API Guardrails

Detail for `AGENTS.md` §3. Obsolete APIs cause compiler warnings, which fail the DoD.
Not listed and unsure? Check the installed source under `Library/PackageCache/`. Do not guess.

| Do not use | Use instead |
|---|---|
| `Rigidbody.velocity` | `Rigidbody.linearVelocity` |
| `Rigidbody.drag`, `Rigidbody.angularDrag` | `Rigidbody.linearDamping`, `Rigidbody.angularDamping` |
| `FindObjectOfType` / `FindObjectsOfType` | Serialized references (lookup via `Find*` is banned by AGENTS.md §4) |
| `UnityEngine.Input.*` (legacy input) | Input System (`InputAction`, `PlayerInput`) |
| `Cinemachine` namespace, `CinemachineVirtualCamera` | `Unity.Cinemachine` namespace, `CinemachineCamera` |
| Custom camera shake code | `CinemachineImpulseSource` / `CinemachineImpulseListener` |
| URP `ScriptableRenderPass.Execute` / `OnCameraSetup` | `RecordRenderGraph` (Render Graph) |

## To Verify

- Check whether `Physics.*NonAlloc` is marked obsolete in the installed version. If it is, update this table and `HotPath.md` together.
