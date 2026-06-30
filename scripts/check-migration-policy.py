#!/usr/bin/env python3
# GymFlow Lite - Migration Policy Linter (HU-017 Phase 6.1)
# Copyright (C) 2026 GymFlow contributors
# License: AGPL v3 (see LICENSE)
#
# Scans EF Core migration .cs files for violations of the additive-only
# migration policy. Exits non-zero on any violation.
#
# Policy rules (ADR-007, HU-017):
#   BLOQUEADO: DropColumn, RenameColumn, incompatible AlterColumn
#   PERMITIDO: AddColumn, CreateTable, CreateIndex, compatible AlterColumn
#
# Usage:
#   python3 scripts/check-migration-policy.py <migrations-directory>
#   python3 scripts/check-migration-policy.py src/backend/Infrastructure/Persistence/Migrations/

import os
import re
import sys


class Violation:
    """A migration policy violation found in a migration file."""

    def __init__(self, file_path: str, line_number: int, operation: str, reason: str):
        self.file_path = file_path
        self.line_number = line_number
        self.operation = operation
        self.reason = reason

    def __str__(self):
        return (
            f"  {self.file_path}:{self.line_number}"
            f"  [{self.operation}] {self.reason}"
        )


# ── Regex patterns (mirrors MigrationPolicy.cs C# implementation) ──────────────
DROP_COLUMN_RE = re.compile(
    r"migrationBuilder\.DropColumn\b",
    re.IGNORECASE,
)

RENAME_COLUMN_RE = re.compile(
    r"migrationBuilder\.RenameColumn\b",
    re.IGNORECASE,
)

# Match the start of an AlterColumn call
ALTER_COLUMN_START_RE = re.compile(
    r"migrationBuilder\.AlterColumn<(.*?)>\(",
    re.IGNORECASE,
)

# Extract oldClrType within an AlterColumn block
OLD_CLR_TYPE_RE = re.compile(
    r"oldClrType\s*:\s*typeof\((\S+)\)",
    re.IGNORECASE,
)

# Extract maxLength values
MAX_LENGTH_RE = re.compile(
    r"maxLength\s*:\s*(-?\d+)",
    re.IGNORECASE,
)

OLD_MAX_LENGTH_RE = re.compile(
    r"oldMaxLength\s*:\s*(null|\d+)",
    re.IGNORECASE,
)


def extract_alter_column_block(lines: list[str], start_index: int) -> str:
    """
    Extract the full multiline block of an AlterColumn call.
    Tracks parentheses depth to find the complete statement.
    """
    block = []
    paren_depth = 0
    started = False

    for j in range(start_index, len(lines)):
        line = lines[j]
        block.append(line)

        paren_depth += line.count("(") - line.count(")")  # net — opening minus closing
        if paren_depth > 0:
            started = True

        if started and paren_depth <= 0:
            break

    return "\n".join(block)


def check_alter_column_compatibility(
    file_path: str, line_number: int, block_text: str
) -> Violation | None:
    """
    Check if an AlterColumn call is compatible with the additive policy.

    INCOMPATIBLE if:
      - Changes the data type (oldClrType != newType)
      - Reduces maxLength
      - Adds maxLength where none existed (oldMaxLength: null)

    COMPATIBLE if:
      - Same type, same or larger maxLength, or only nullable change
    """
    alter_match = ALTER_COLUMN_START_RE.search(block_text)
    if not alter_match:
        return None

    new_type = alter_match.group(1).strip()

    # ── Type change check ──────────────────────────────────────────────────
    old_type_match = OLD_CLR_TYPE_RE.search(block_text)
    if old_type_match:
        old_type = old_type_match.group(1).strip()
        if old_type.lower() != new_type.lower():
            return Violation(
                file_path,
                line_number,
                "AlterColumn",
                f"Cambio de tipo incompatible: '{old_type}' -> '{new_type}'. "
                "Los cambios de tipo pueden causar pérdida de datos o errores de conversion.",
            )

    # ── maxLength reduction check ──────────────────────────────────────────
    new_max_match = MAX_LENGTH_RE.search(block_text)
    old_max_match = OLD_MAX_LENGTH_RE.search(block_text)

    if new_max_match and old_max_match:
        new_max = int(new_max_match.group(1))
        old_max_str = old_max_match.group(1)

        # Adding maxLength where none existed = data restriction
        if old_max_str == "null":
            return Violation(
                file_path,
                line_number,
                "AlterColumn",
                "Agregar maxLength donde antes no existia es una restriccion de datos. "
                "Puede causar pérdida de datos por truncamiento.",
            )

        old_max = int(old_max_str)
        if new_max < old_max:
            return Violation(
                file_path,
                line_number,
                "AlterColumn",
                f"Reduccion de maxLength: {old_max} -> {new_max}. "
                "Puede causar pérdida de datos por truncamiento.",
            )

    return None


def validate_file(file_path: str) -> list[Violation]:
    """Validate a single migration .cs file against the additive policy."""
    violations: list[Violation] = []

    if not os.path.isfile(file_path):
        return violations

    with open(file_path, "r", encoding="utf-8") as f:
        lines = f.readlines()

    for i, line in enumerate(lines):
        line_number = i + 1

        # ── DropColumn → SIEMPRE bloqueado ──────────────────────────────────
        if DROP_COLUMN_RE.search(line):
            violations.append(
                Violation(
                    file_path,
                    line_number,
                    "DropColumn",
                    "Eliminar columnas causa pérdida irreversible de datos. "
                    "Marcar la columna como obsoleta en su lugar.",
                )
            )
            continue

        # ── RenameColumn → SIEMPRE bloqueado ────────────────────────────────
        if RENAME_COLUMN_RE.search(line):
            violations.append(
                Violation(
                    file_path,
                    line_number,
                    "RenameColumn",
                    "Renombrar columnas rompe referencias semánticas. "
                    "Agregar una nueva columna y migrar datos, luego marcar la anterior como obsoleta.",
                )
            )
            continue

        # ── AlterColumn → bloqueado si es incompatible ──────────────────────
        if "migrationBuilder.AlterColumn<" in line:
            block_text = extract_alter_column_block(lines, i)
            violation = check_alter_column_compatibility(file_path, line_number, block_text)
            if violation:
                violations.append(violation)

    return violations


def validate_directory(migrations_dir: str) -> list[Violation]:
    """Scan all .cs migration files in a directory, excluding Designer files."""
    violations: list[Violation] = []

    if not os.path.isdir(migrations_dir):
        print(
            f"WARNING: Migrations directory not found: {migrations_dir}",
            file=sys.stderr,
        )
        return violations

    cs_files = sorted(
        f
        for f in os.listdir(migrations_dir)
        if f.endswith(".cs") and not f.endswith(".Designer.cs")
    )

    if not cs_files:
        print(
            f"WARNING: No migration .cs files found in {migrations_dir}",
            file=sys.stderr,
        )
        return violations

    for filename in cs_files:
        file_path = os.path.join(migrations_dir, filename)
        file_violations = validate_file(file_path)
        violations.extend(file_violations)

    return violations


def main() -> int:
    if len(sys.argv) < 2:
        print(f"Usage: {sys.argv[0]} <migrations-directory>", file=sys.stderr)
        print("Example: python3 scripts/check-migration-policy.py src/backend/Infrastructure/Persistence/Migrations/", file=sys.stderr)
        return 1

    migrations_dir = sys.argv[1]
    print(f"🔍 Scanning migration policy in: {migrations_dir}")
    print()

    violations = validate_directory(migrations_dir)

    total_files = len(
        [
            f
            for f in os.listdir(migrations_dir)
            if f.endswith(".cs") and not f.endswith(".Designer.cs")
        ]
    ) if os.path.isdir(migrations_dir) else 0

    print(f"  Files scanned: {total_files}")

    if not violations:
        print("  ✅ All migration files comply with the additive-only policy.")
        return 0

    print(f"  ❌ Found {len(violations)} policy violation(s):")
    print()
    for v in violations:
        print(v)
        print()

    print("──")
    print("Policy rules (ADR-007, HU-017):")
    print("  ❌ DropColumn    — elimina datos irreversiblemente")
    print("  ❌ RenameColumn  — rompe referencias semánticas")
    print("  ❌ AlterColumn incompatible — cambio de tipo, reducción de maxLength")
    print()
    print("Alternativas:")
    print("  • Deprecar columnas en lugar de eliminarlas")
    print("  • Agregar nueva columna, migrar datos, luego marcar la anterior")
    print("  • Aumentar maxLength en lugar de reducirlo")
    print("  • Usar CreateTable + CreateIndex para nuevas tablas")

    return 1


if __name__ == "__main__":
    sys.exit(main())
