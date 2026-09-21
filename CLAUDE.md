# CardBattlerGame

Unity 2D card battler. **Code is written by hand.** AI is used for code review, analysis, and
low-level / performance fixes only — never to author features. Propose changes with reasoning and
let the developer approve them; do not write gameplay code unless explicitly asked.

**Never add code comments.** Not explanatory comments, not `//`-notes justifying a decision, not
XML doc comments, not TODOs — in new code or in code you edit. The reasoning belongs in the
reply to the developer, where it can be discussed, not in the file, where it rots. Comments in
this codebase are written by the developer alone. Existing comments stay untouched unless the
change makes them wrong.

## Stack

Unity 6 (URP, 2D) · VContainer 1.19 (DI) · UniTask (async) · one asmdef per scope+layer.

## Structure

`Assets/Scripts/<Scope>/<Layer>/` — assembly `Game.<Scope>.<Layer>`.

- **Scopes**: `EntryPoint`, `Loading`, `Meta`, `Core`, `Shared`
- **Layers**: `Model`, `Config`, `Service`, `ViewModel`, `DI`

Dependency direction (asmdef references are the enforcement mechanism):

| Layer | May reference |
|---|---|
| `Model` | nothing |
| `Config` | own `Model`, `Shared.Model` (Unity-dependent — holds `ScriptableObject` settings assets) |
| `Service` | own `Model`, own or `Shared` `Config`, `Shared.Model`, `Shared.Service` |
| `ViewModel` | own `Model`, own `Service` (Unity-dependent) |
| `DI` | every layer of its own scope, plus `Shared.Config` / `Shared.Service` — the only place that wires dependencies |

Scopes never reference each other. Code needed by two scopes moves to `Shared`.

`Config` is where settings ScriptableObjects live, and nothing else: no logic, no behaviour, no
mutable state. A config type exposes its serialized fields as read-only properties and is authored as
an asset under `Assets/Configs/`. The asset is assigned on the `LifetimeScope` prefab and registered
with `RegisterInstance` by `DI`; a `Service` receives it as a constructor dependency and never loads
it itself. The layer exists separately from `Model` because a `ScriptableObject` drags in UnityEngine,
and `Model` must stay reference-free.

A `ViewModel` is a `MonoBehaviour` that lives in a scene, so the container does not construct it —
it only injects into it. Such a component is bound through the `autoInjectGameObjects` list on that
scene's `LifetimeScope`, not through a `Register` call in `Configure`. That is the intended wiring:
it keeps ViewModel types `internal` and keeps a scope's `DI` assembly from referencing its
`ViewModel` assembly at all. "`DI` is the only place that wires dependencies" governs the objects
the container builds — services, configs, models; scene components are wired in the scene.

## Core requirements

### 1. Clean, low-coupled architecture

Components and scopes stay independent. **No interface until it is actually needed** — i.e. until a
cross-scope dependency must be inverted, or a second implementation exists. A single-implementation
`IFooService` next to `FooService` is noise and should be removed.

A `Shared` type consumed by more than one scope **is** such a cross-scope dependency: its interface
stays, single implementation or not. It keeps the implementation `internal` to its own assembly, so
the consuming scopes depend on the contract rather than on `Shared` internals. `ISceneTransitionService`
and `ISceneTransitionProgressModel` are the reference case. "Only one implementation" alone is never a
reason to remove such an interface — the test is whether the type crosses a module boundary.

### 2. Stability — the game loop never stops

- Meaningful validation at every boundary that can receive bad data (save data, config/ScriptableObjects,
  network, user input). Validation must be meaningful: reject and report, don't just null-check and continue silently.
- The main flow is exception-free. Failures are represented in return values/state, not thrown.
- No unhandled exception may ever break the game loop. Long-running loops and async entry points
  contain their failure.
- **One deliberate exception**: invalid DI configuration may throw. A valid VContainer setup is the
  developer's responsibility and is expected to fail loudly and immediately at composition time.

### 3. Performance as a feature

Optimize performance-critical paths (per-frame, per-card, battle resolution) — avoid per-frame
allocation, `GetComponent` in hot paths, LINQ in loops, boxing, repeated `Camera.main`. Outside those
paths, clarity wins: do not obfuscate maintainable code for micro-gains.

### 4. Async-based

UniTask is the async primitive. Prefer `async UniTask` over callbacks/events whenever the flow is
sequential and expressible as `await`. Pass `CancellationToken` through async call chains and tie it
to object lifetime (`this.GetCancellationTokenOnDestroy()` / VContainer scope). Never `async void`
(use `UniTaskVoid` + `.Forget()`); never `.Result`/`.Wait()`.

**A project-scoped service that owns a cross-scene operation is the exception**: it cancels on its
own `CancellationTokenSource`, disposed with the container, and accepts no token from the caller.
`SceneTransitionService` is the reference case. `GoTo` and `ActivatePendingTarget` deliberately
outlive the caller — the ViewModel that started the transition is destroyed the moment its scene
unloads, so threading its destroy token in would cancel the transition halfway between source,
loading and target scene. `LoadPendingTarget` takes no token for a different reason: the caller's
token expires only on destroy, which for the loading scene means application exit, and that is
already what the service's own lifetime token covers, while an ordinary transition away is handled
by the service's re-entrancy state. The rule is lifetime, not habit — pass a token through chains
whose work belongs to the caller, and leave it out where the caller is not what the work outlives.

### 5. Nullable context

Nullable reference types are **enabled in every assembly**, via a `csc.rsp` containing
`-nullable:enable` placed next to the asmdef. `Model`, `Config`, `Service`, `ViewModel` and `DI` all
get one — there is no exempt layer.

**Every new assembly ships its own `csc.rsp` in the same commit as its asmdef.** Unity applies a
`csc.rsp` only to the assembly whose asmdef sits beside it; a single `Assets/csc.rsp` reaches the
predefined assemblies only and silently does nothing for asmdef-based code, which makes every `?`
annotation in it unchecked. An asmdef without a sibling `csc.rsp` is a missing file, not a style
choice.

Do not sprinkle `!` to silence the compiler — model absence explicitly. The one accepted use is
`= null!` on a `[SerializeField]` or `[Inject]`-assigned field, where the engine or the container,
not the constructor, is what guarantees the value. It is required on those fields rather than merely
tolerated: without it, Rider infers the field is never assigned and therefore always null, and starts
reporting every guard on it as unreachable code.

## Reviewing

`/review` runs the three-reviewer pass (architecture / QA / performance) against the working-tree diff.
See `.claude/skills/review/SKILL.md`.
