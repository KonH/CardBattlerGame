# Review contract

Shared by all reviewer agents. Read `CLAUDE.md` for the project's core requirements — that file is
the rubric. This file defines priorities and the output format.

## Priorities

| Priority | Means |
|---|---|
| **Critical** | Stability, failure-safety, null-safety. Anything that can throw in the main flow, stall the game loop, or dereference null. |
| **Major** | Architecture, core requirements (async/UniTask, nullable context, premature interfaces, scope coupling), Unity API misuse. |
| **Minor** | Everything else — naming, readability, redundancy, small non-hot-path inefficiency. |

An invalid **DI configuration throwing is intentional**, not a finding.

## Rules for every finding

- Report only what you can point at in the reviewed code. Do not speculate about files you did not read.
- One finding per real problem. No stylistic preferences, no "consider maybe".
- If the same root cause appears in several places, report it once and list the other sites.
- Never propose adding an interface unless a cross-scope dependency needs inverting or a second
  implementation exists.
- Never propose *removing* an interface that a second scope consumes. A `Shared` contract used by
  more than one scope is a cross-scope dependency and stays, single implementation or not —
  "only one implementation" is not on its own a finding.
- When a diff adds or moves an asmdef, check for a `csc.rsp` containing `-nullable:enable` beside it.
  Every assembly must have one, with no exempt layer; a missing or misplaced `csc.rsp` (e.g. a single
  one at `Assets/`, which reaches no asmdef assembly) is Major — it makes every `?` in that assembly
  unchecked. `= null!` on a `[SerializeField]`/`[Inject]` field is correct and is never a finding.
- A project-scoped service that owns a **cross-scene** async operation cancels on its own lifetime
  `CancellationTokenSource` and takes no `CancellationToken` from the caller — `SceneTransitionService`
  (`GoTo`, `LoadPendingTarget`, `ActivatePendingTarget`) is the reference case. A caller token there
  would expire mid-transition or add nothing over the service's own. This is intentional, not a
  missing parameter, and is never a finding. A missing token on a chain whose work *does* belong to
  the caller's lifetime still is.
- A `ViewModel` `MonoBehaviour` bound through `autoInjectGameObjects` on its scene's `LifetimeScope`
  is wired as intended. Do not propose `RegisterComponentInHierarchy<T>()`, making the type `public`,
  or adding a `ViewModel` reference to a `DI` asmdef; the container injects into scene components, it
  does not construct them.
- **No comments in patches.** A `PATCH` block never introduces a code comment, an XML doc comment
  or a TODO, and "add a comment explaining X" is never a finding. Put the explanation in the `WHY`
  line instead. If a patch only makes sense with a comment, the patch is not clear enough.
- If you find nothing in your lens, say so. An empty report is a valid and useful result.
- The reviewed code was written by hand by the developer. Be direct and technical, not deferential,
  and do not rewrite working code for taste.

## Output format

Output nothing but the blocks below — no preamble, no summary, no closing remarks.

For each concrete, inline-fixable problem:

```
FIX | <Critical|Major|Minor> | <path>:<line> | <title, max 8 words>
WHY: <1-2 sentences: what breaks or what it costs, concretely>
PATCH:
<<<OLD
<exact current code>
OLD
<<<NEW
<exact replacement code>
NEW
```

`PATCH` must be a literal, applicable replacement — `OLD` must match the file byte-for-byte.
If a problem is real but has no single-site mechanical fix, use `PATCH: none` and put the
required change in one sentence under `WHY`.

For each project-level observation that is not a single-site fix (architecture, consistency,
extensibility) — at most **2** per agent, `Critical`/`Major` only:

```
HINT | <Critical|Major> | <title, max 8 words>
<2-4 sentences: the pattern you saw, why it conflicts with a core requirement, and the rule to
follow going forward. Concrete, name the files/types involved.>
```
