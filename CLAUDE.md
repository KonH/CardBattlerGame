# CardBattlerGame

Unity 2D card battler. **Code is written by hand.** AI is used for code review, analysis, and
low-level / performance fixes only — never to author features. Propose changes with reasoning and
let the developer approve them; do not write gameplay code unless explicitly asked.

## Stack

Unity 6 (URP, 2D) · VContainer 1.19 (DI) · UniTask (async) · one asmdef per scope+layer.

## Structure

`Assets/Scripts/<Scope>/<Layer>/` — assembly `Game.<Scope>.<Layer>`.

- **Scopes**: `EntryPoint`, `Loading`, `Meta`, `Core`, `Shared`
- **Layers**: `Model`, `Service`, `ViewModel`, `DI`

Dependency direction (asmdef references are the enforcement mechanism):

| Layer | May reference |
|---|---|
| `Model` | nothing |
| `Service` | own `Model`, `Shared.Service` |
| `ViewModel` | own `Model`, own `Service` (Unity-dependent) |
| `DI` | every layer of its own scope — the only place that wires dependencies |

Scopes never reference each other. Code needed by two scopes moves to `Shared`.

## Core requirements

### 1. Clean, low-coupled architecture

Components and scopes stay independent. **No interface until it is actually needed** — i.e. until a
cross-scope dependency must be inverted, or a second implementation exists. A single-implementation
`IFooService` next to `FooService` is noise and should be removed.

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

### 5. Nullable context

Nullable reference types are **enabled in the `Service` layer** (and `Model`), via a `csc.rsp`
containing `-nullable:enable` next to the asmdef. Not enabled in `DI` or `ViewModel`, which are
Unity-dependent and interoperate with Unity's non-annotated API. Do not sprinkle `!` to silence the
compiler — model absence explicitly.

## Reviewing

`/review` runs the three-reviewer pass (architecture / QA / performance) against the working-tree diff.
See `.claude/skills/review/SKILL.md`.
