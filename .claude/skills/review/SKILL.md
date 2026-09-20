---
name: review
description: Multi-agent code review for this Unity project. Runs three reviewers in parallel — architecture (Opus), QA/safety (Sonnet), performance (Sonnet) — against the working-tree diff or a given branch, PR or path, then reports prioritized insights and fixes and applies the ones you approve. Use when the user asks to review code, check a diff before committing, or invokes /review.
allowed-tools: Agent, Read, Grep, Glob, Bash, Edit, AskUserQuestion
---

# Multi-agent review

Three reviewers, one lens each, run **in parallel**. You orchestrate: resolve the target, dispatch,
merge, report, ask, apply. You do not review the code yourself — you judge and merge what comes back.

## 1. Resolve the target

| Argument | Target |
|---|---|
| *(none)* | `git diff HEAD` — staged + unstaged working-tree changes |
| branch name | `git diff <base>...<branch>` |
| `#123` / PR URL | that PR's diff (GitHub MCP tools) |
| path | every `.cs` file under that path |

```bash
git -C . status --short && git -C . diff HEAD --stat
```

If the diff is empty, say so and stop — offer `HEAD~1`, a branch, or a path instead. Do not silently
review something the user did not ask for.

Write the diff to a file under the scratchpad directory so the agents read it instead of you pasting
it into three prompts. Note the paths of the touched `.cs` files.

## 2. Dispatch all three, in one message

Send all three `Agent` calls **in a single assistant message** so they run concurrently. Background
them (`run_in_background: true`) — you have nothing to do until all three return.

- `subagent_type: "review-architect"` — high-level architecture, dependency usage, core requirements
- `subagent_type: "review-qa"` — null-safety, exception risk, validation, logic bugs
- `subagent_type: "review-perf"` — allocation, hot paths, Unity API cost

Each prompt contains only this, filled in:

```
Review the changes described below against this project's core requirements.

Diff: <scratchpad path>
Touched files: <paths>
Target: <what was reviewed, e.g. "working-tree diff, 4 files">

Read .claude/skills/review/CONTRACT.md and CLAUDE.md first — they define the rubric, the priority
levels and the output format you must follow. Read the touched files, not only the diff hunks.
Report findings in your lens only. Output nothing but FIX and HINT blocks.
```

Never add lens instructions of your own — each agent's role lives in its definition file.

## 3. Merge

When all three return:

1. **Dedupe.** Same file, same line, same root cause → keep the one with the sharper `WHY` and the
   higher priority. Cross-lens overlap is expected (a null deref in an `Update` loop reaches both QA
   and performance); it is never reported twice.
2. **Re-judge priority yourself.** Agents over- and under-rate. Apply the contract's table:
   Critical = stability / failure-safety / null-safety; Major = architecture, core requirements,
   Unity API misuse; Minor = everything else. A DI-configuration throw is not a finding — drop it.
3. **Verify before reporting.** Open the file and check each finding is real and each `PATCH` `OLD`
   block still matches byte-for-byte. Drop anything you cannot confirm in the code; a wrong finding
   costs more than a missed one.

## 4. Budget — enforce strictly

**Hints:** at most **3**, `Critical`/`Major` only, chosen for highest leverage. Never restate a fix.
Drop the rest, including all `Minor` hints.

**Fixes:** every verified `Critical` and `Major`, never truncated. Then append `Minor` fixes,
most valuable first, **only while the list is shorter than 10** — stop at 10. If Critical + Major
already reaches 10 or more, report no `Minor` at all.

## 5. Report

Concise, chat only. No file written, nothing posted anywhere.

````
## Review — <target> · <N> files

### Insights
**[Major] <title>** — <2-4 sentences: the pattern, the requirement it conflicts with, the rule going forward.>

### Fixes
**1. [Critical] `<path>:<line>` — <title>**
<1-2 sentences: what breaks and what the change fixes.>
```diff
- <old>
+ <new>
```
````

Numbered continuously across priorities, Critical first. One finding, one short paragraph, one diff —
no tables of findings, no restating the code, no closing summary. If a section is empty, write
`### Insights` / `_none_` rather than padding it.

## 6. Ask, then apply

After the full report, ask for approval in `AskUserQuestion` batches: **one question per priority
tier**, Critical first, `multiSelect: true`, at most 4 fixes per question (split a tier across
questions if needed). Each option label is `<number>. <title>`; each description is the one-line
reason. Never apply anything before the user answers.

Apply only what was selected, with `Edit`, exactly as shown in the report — no extra changes, no
opportunistic cleanup, no reformatting. If a patch no longer applies, say so and skip it.

Close with one line: what was applied, what was skipped. Do not commit — this project's code is
hand-written and the developer owns the commit.
