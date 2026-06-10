# ADR-005: Naming Conventions

**Status**: 🟢 Accepted
**Date**: 2026-06-09
**Deciders**: @gymflow-tech-lead

---

## Context

GymFlow Lite has multiple document types (HU, ADR, RFC, PRD, template) that live in different folders. Without explicit naming rules, we end up with files like `HU-1.md`, `hu01_checkin.md`, `checkin.md`, `HU_01.md` — all referring to the same thing. We need conventions that:

- Are **predictable**: a developer can guess the name of any document.
- Are **grep-friendly**: searching for `HU-013` returns only the HU-013 files.
- Are **sort-friendly**: alphabetical sort = chronological sort.
- Are **cross-platform**: no characters that cause issues on Windows, macOS, or Linux filesystems.
- Are **language-agnostic**: the convention works whether the team writes in Spanish, English, or both.

---

## Decision

### Format per document type

| Type | Format | Example |
|------|--------|---------|
| HU | `HU-NNN-name.md` (3 digits, uppercase) | `HU-013-cicd.md` |
| ADR | `ADR-NNN-name.md` (3 digits) | `ADR-001-stack-tecnologico.md` |
| RFC | `RFC-NNN-name.md` (3 digits) | `RFC-001-arquitectura-offline.md` |
| PRD | `PRD_ProductName.md` (underscore) | `PRD_GymFlow_Lite.md` |
| Template | `template-type.md` (kebab-case) | `template-hu-simple.md` |

### Detailed rules

1. **Numbering**:
   - Always 3 digits with leading zero (`HU-013`, not `HU-13` or `HU-13-cicd`).
   - Sequential, never reused (deleted HUs leave a gap; do not renumber).

2. **Case**:
   - Prefix is uppercase (`HU-`, `ADR-`, `RFC-`, `PRD_`).
   - Description is lowercase kebab-case (`cicd`, `stack-tecnologico`).

3. **Separators**:
   - `kebab-case` (hyphens) for the descriptive part.
   - Underscore (`PRD_Name.md`) is the only allowed underscore in filenames, used only for the PRD's product name.
   - No spaces.

4. **File extension**:
   - `.md` (Markdown) for all text documents.
   - `.sh` for shell scripts (in `docs/others/`).
   - Never use `.MD`, `.txt`, or other extensions.

5. **Special characters**:
   - No accents in filenames (e.g., `autenticacion` not `autenticación`).
   - No `ñ` (use `n` instead).
   - No emojis.

### Examples

✅ Correct:
```
HU-013-cicd.md
HU-014-nueva-feature.md
ADR-001-stack-tecnologico.md
PRD_GymFlow_Lite.md
template-hu-simple.md
```

❌ Wrong:
```
HU13-cicd.md             (missing hyphen, 2 digits)
HU-13-cicd.md            (2 digits, should be 3)
hu-013-cicd.md           (lowercase prefix)
HU-013_CICD.md           (underscore in description)
HU-013-cicd.MD           (uppercase extension)
HU-013-cicd.md           (no description, just number)
HU-013-cicd nuevo.md    (space in name)
HU-013-autenticación.md  (accent in name)
```

---

## Options Considered

### Option 1: camelCase ❌

**Pros**:
- Native to JavaScript/TypeScript devs.

**Cons**:
- **Not case-sensitive on macOS and Windows by default** (HFS+ and NTFS are case-insensitive; only Linux is case-sensitive). This causes git conflicts and "works on my machine" issues.
- Looks weird mixed with hyphens.

### Option 2: snake_case ❌

**Pros**:
- Common in Python and Ruby.

**Cons**:
- Underscores are harder to read in long names.
- We use hyphens everywhere else (URLs, git branches, etc.); snake_case is inconsistent.

### Option 3: Free naming ❌

**Pros**:
- Maximum flexibility per author.

**Cons**:
- Inconsistent across the repo.
- Hard to grep, hard to sort.
- New devs cannot predict names.

### Option 4: Kebab-case with strict format ✅ (chosen)

**Pros**:
- Grep-friendly: `rg "HU-013"` returns exactly the HU-013 files.
- Sort-friendly: `ls` alphabetical order matches HU number order (after the 3-digit padding).
- URL-friendly: works directly in static site generators and web URLs.
- Consistent across all document types (only the prefix and separator vary).

**Cons**:
- Requires devs to remember "3 digits, kebab-case, no accents".
- The audit script (`docs/others/flowdoc-audit.sh`) flags violations; devs must fix them.

---

## Consequences

### Positive

- Consistent, predictable filenames.
- Grep and sort work as expected.
- Cross-platform safe (no case-sensitivity issues, no special chars).

### Negative

- Authors must remember the rules; mitigated by the audit script and AGENTS.md.
- Existing files in `docs/technical/` use a slightly different format (`hu01-checkin.md` lowercase prefix, 2 digits). They are grandfathered in and not renamed.

### Neutral

- Branch names follow a similar convention but allow more flexibility (see AGENTS.md commit conventions).
- The FlowDocs audit script (`docs/others/flowdoc-audit.sh`) is the source of truth for naming checks.

---

## References

- ADR-004: Documentation Structure
- FlowDocs audit script: `docs/others/flowdoc-audit.sh` (Check 4: Naming Consistency)
- AGENTS.md section 8 (operational naming rules)
