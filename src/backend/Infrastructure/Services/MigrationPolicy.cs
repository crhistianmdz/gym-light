// GymFlow Lite - Schema Versioning (HU-017)
// Copyright (C) 2026 GymFlow contributors
// License: AGPL v3 (see LICENSE)

using System.Text.RegularExpressions;

namespace GymFlow.Infrastructure.Services;

/// <summary>
/// Valida archivos de migración EF Core contra la política aditiva.
/// 
/// La política aditiva (ADR-007, HU-017) dicta que las migraciones no deben
/// eliminar ni modificar destructivamente datos existentes. Esto garantiza
/// compatibilidad hacia atrás y permite upgrades sin pérdida de datos.
/// 
/// Operaciones BLOQUEADAS (causan pérdida de datos):
/// - DropColumn: elimina una columna y sus datos
/// - RenameColumn: rompe referencias semánticas (sin migración de datos explícita)
/// - AlterColumn incompatible: cambia tipo de columna o reduce maxLength
/// 
/// Operaciones PERMITIDAS:
/// - AddColumn (nueva columna con default)
/// - CreateTable (nueva tabla)
/// - CreateIndex (nuevo índice)
/// - AlterColumn compatible (solo aumenta nullable, aumenta maxLength)
/// </summary>
public class MigrationPolicy
{
    /// <summary>
    /// Resultado de validación de una migración individual.
    /// </summary>
    public record PolicyViolation(
        string FilePath,
        int LineNumber,
        string Operation,
        string Reason
    );

    /// <summary>
    /// Patrones de operaciones bloqueadas en archivos de migración .cs.
    /// Cada patrón captura la operación, tabla y columna cuando es posible.
    /// </summary>
    private static readonly Regex DropColumnPattern = new(
        @"migrationBuilder\.DropColumn\b",
        RegexOptions.Compiled);

    private static readonly Regex RenameColumnPattern = new(
        @"migrationBuilder\.RenameColumn\b",
        RegexOptions.Compiled);

    private static readonly Regex AlterColumnPattern = new(
        @"migrationBuilder\.AlterColumn<[^>]+>\(\s*\n?\s*name:\s*""(?<name>[^""]+)""[^)]*?(?:oldClrType|oldType)\s*:\s*typeof\((?<oldType>[^)]+)\)[^)]*?\)",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex AlterColumnTypeChangePattern = new(
        @"migrationBuilder\.AlterColumn<(?<newType>[^>]+)>[^;]*?oldClrType:\s*typeof\((?<oldType>[^)]+)\)",
        RegexOptions.Compiled | RegexOptions.Singleline);

    private static readonly Regex OldMaxLengthPattern = new(
        @"oldMaxLength:\s*(?<value>\d+|null)",
        RegexOptions.Compiled);

    private static readonly Regex MaxLengthPattern = new(
        @"maxLength:\s*(?<value>\d+)",
        RegexOptions.Compiled);

    /// <summary>
    /// Valida todos los archivos .cs de migración en el directorio especificado.
    /// Solo analiza archivos que NO sean Designer.cs.
    /// </summary>
    /// <param name="migrationsDirectory">Ruta al directorio de migraciones.</param>
    /// <returns>Lista de violaciones encontradas. Vacía si todo cumple la política.</returns>
    public Task<IReadOnlyList<PolicyViolation>> ValidateDirectoryAsync(string migrationsDirectory)
    {
        var violations = new List<PolicyViolation>();

        if (!Directory.Exists(migrationsDirectory))
            return Task.FromResult<IReadOnlyList<PolicyViolation>>(violations);

        var migrationFiles = Directory.GetFiles(migrationsDirectory, "*.cs")
            .Where(f => !f.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase))
            .OrderBy(f => f);

        foreach (var filePath in migrationFiles)
        {
            var fileViolations = ValidateFile(filePath);
            violations.AddRange(fileViolations);
        }

        return Task.FromResult<IReadOnlyList<PolicyViolation>>(violations);
    }

    /// <summary>
    /// Valida un archivo de migración individual.
    /// </summary>
    public IReadOnlyList<PolicyViolation> ValidateFile(string filePath)
    {
        var violations = new List<PolicyViolation>();

        if (!File.Exists(filePath))
            return violations;

        var lines = File.ReadAllLines(filePath);

        for (int i = 0; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            var line = lines[i];

            // ── DropColumn → SIEMPRE bloqueado ──
            if (DropColumnPattern.IsMatch(line))
            {
                violations.Add(new PolicyViolation(
                    filePath, lineNumber, "DropColumn",
                    "Eliminar columnas causa pérdida irreversible de datos. " +
                    "Marcar la columna como obsoleta en su lugar."));
                continue;
            }

            // ── RenameColumn → SIEMPRE bloqueado ──
            if (RenameColumnPattern.IsMatch(line))
            {
                violations.Add(new PolicyViolation(
                    filePath, lineNumber, "RenameColumn",
                    "Renombrar columnas rompe referencias semánticas. " +
                    "Agregar una nueva columna y migrar datos, luego marcar la anterior como obsoleta."));
                continue;
            }

            // ── AlterColumn → solo bloqueado si es incompatible ──
            if (line.Contains("migrationBuilder.AlterColumn<"))
            {
                var blockText = GetAlterColumnBlock(lines, i);
                var violation = CheckAlterColumnCompatibility(filePath, lineNumber, blockText);
                if (violation != null)
                    violations.Add(violation);
            }
        }

        return violations;
    }

    /// <summary>
    /// Extrae el bloque completo de una llamada a AlterColumn (multilínea).
    /// </summary>
    private static string GetAlterColumnBlock(string[] lines, int startIndex)
    {
        var block = new System.Text.StringBuilder();
        int parenDepth = 0;
        bool started = false;

        for (int j = startIndex; j < lines.Length; j++)
        {
            var l = lines[j];
            block.AppendLine(l);

            foreach (char c in l)
            {
                if (c == '(') { parenDepth++; started = true; }
                if (c == ')') parenDepth--;
            }

            if (started && parenDepth <= 0)
                break;
        }

        return block.ToString();
    }

    /// <summary>
    /// Verifica si un AlterColumn es compatible con la política aditiva.
    /// 
    /// Un AlterColumn es INCOMPATIBLE si:
    /// - Cambia el tipo de datos (oldClrType != newType)
    /// - Reduce el maxLength
    /// 
    /// Es COMPATIBLE si:
    /// - Solo cambia nullable de false a true
    /// - Solo aumenta maxLength
    /// </summary>
    private static PolicyViolation? CheckAlterColumnCompatibility(
        string filePath, int lineNumber, string blockText)
    {
        // Detectar cambio de tipo de dato
        var typeMatch = AlterColumnTypeChangePattern.Match(blockText);
        if (typeMatch.Success)
        {
            var newType = typeMatch.Groups["newType"].Value;
            var oldType = typeMatch.Groups["oldType"].Value;

            // Normalizar tipos para comparación (quitar espacios)
            var newTypeNormalized = newType.Trim();
            var oldTypeNormalized = oldType.Trim();

            if (!string.Equals(newTypeNormalized, oldTypeNormalized, StringComparison.OrdinalIgnoreCase))
            {
                return new PolicyViolation(
                    filePath, lineNumber, "AlterColumn",
                    $"Cambio de tipo incompatible: '{oldTypeNormalized}' → '{newTypeNormalized}'. " +
                    "Los cambios de tipo pueden causar pérdida de datos o errores de conversión.");
            }
        }

        // Detectar reducción de maxLength
        var maxMatch = MaxLengthPattern.Match(blockText);
        var oldMaxMatch = OldMaxLengthPattern.Match(blockText);

        if (maxMatch.Success && oldMaxMatch.Success)
        {
            var oldMaxStr = oldMaxMatch.Groups["value"].Value;

            // Si oldMaxLength es null, no había límite antes → cualquier límite nuevo es reducción
            if (oldMaxStr == "null")
            {
                return new PolicyViolation(
                    filePath, lineNumber, "AlterColumn",
                    "Agregar maxLength donde antes no existía es una restricción de datos. " +
                    "Puede causar pérdida de datos por truncamiento.");
            }

            if (int.TryParse(maxMatch.Groups["value"].Value, out int newMax) &&
                int.TryParse(oldMaxStr, out int oldMax) &&
                newMax < oldMax)
            {
                return new PolicyViolation(
                    filePath, lineNumber, "AlterColumn",
                    $"Reducción de maxLength: {oldMax} → {newMax}. " +
                    "Puede causar pérdida de datos por truncamiento.");
            }
        }

        // Si llegamos acá: AlterColumn es compatible (mismo tipo, mismo o mayor maxLength, o solo cambia nullable)
        return null;
    }

    /// <summary>
    /// Devuelve true si no hay violaciones en el directorio especificado.
    /// </summary>
    public async Task<bool> IsValidAsync(string migrationsDirectory)
    {
        var violations = await ValidateDirectoryAsync(migrationsDirectory);
        return violations.Count == 0;
    }
}
