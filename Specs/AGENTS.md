# Writing SPECs

Applies to every `Specs/<Module>/SPEC.md`. A SPEC is the contract the user approves, and tests are derived from its ACs. Anything the SPEC leaves undecided gets decided silently inside a test, where the user has to read code to find it. Write SPECs so that does not happen.

## Structure
- Exactly four sections: 개요 / 기능 요구 사항 / 인수 기준 / 제외 범위. Do not add sections.
- Title: the system's common genre name (e.g. "공격 시스템"). Written in Korean.

## Functional requirements (FR)
- Derive them top-down from design intent: GDD pillar → GDD rules → requirement. Start from the core function the user states in plain words; do not list researched details.
- The subject is the system ("공격 시스템은 ~한다"). No implementation identifiers or means: class, method, field, or assembly names, `Tick`, `dt`, frame loops.
- Do not add implementation choices or usage scenarios (notification mechanism, initial state) as requirements.
- One requirement, one sentence, in the order "(~인 동안,) (~하면,) <시스템>은 ~한다". Split a sentence that mixes behaviors.
- State what this system decides and reports. What another system does with it belongs in that system's SPEC.
- Numbers (durations, speeds, thresholds) are data, not requirements. A requirement may say that a value comes from data.

## Acceptance criteria (AC)
- One observable outcome each, observed through this system's outputs, ending with `(FR-n)`. Every FR has at least one AC.
- The "~한 상태에서" clause states the full initial state the check needs.
- Use symbols for data values, never fixed numbers: "Windup 길이가 W인 공격을 시작한 뒤 ...".
- Write time boundaries with "미만" and "이상(이르면)". Every boundary gets both ACs: just before it and at it. Never use "짧은", "긴", "직후" for a boundary.
- Combination rules (state × input) may be written as a table. Each cell is one AC with its number and FR. A table has no empty cells.
- Default verification is an automated EditMode test with no scene or asset. Mark an AC `(manual)` only when the user approves it with the SPEC; use it for feel and visuals, not for rules.
- An AC never leaves a decision to the test writer. If you cannot state the expected result, ask the user.

## Out of scope
- List only what implementing the FRs could drift into. It is not a list of other systems' responsibilities.

## Process
- Propose one section at a time (개요 → 기능 요구 사항 → 인수 기준 → 제외 범위), each sentence with its GDD reason. Only sentences the user confirms go into the file.
- The user decides when a discussion is finished. Do not end replies by announcing or hurrying the next step.
- An answer to one question is not approval to write. When all four sections are settled, show the full SPEC and write the file only after an explicit go.
- Changing the meaning of an approved AC requires the user's approval.
