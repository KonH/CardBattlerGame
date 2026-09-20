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
