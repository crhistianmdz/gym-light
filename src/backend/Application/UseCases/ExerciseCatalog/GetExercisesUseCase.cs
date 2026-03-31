using GymFlow.Application.DTOs;
using GymFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GymFlow.Application.UseCases.ExerciseCatalog;

public class GetExercisesUseCase(GymFlowDbContext db)
{
    public async Task<List<ExerciseCatalogItemDto>> ExecuteAsync(string? nameFilter, CancellationToken ct = default)
    {
        var query = db.ExerciseCatalogs.AsQueryable();

        if (!string.IsNullOrWhiteSpace(nameFilter))
            query = query.Where(e => e.Name.ToLower().Contains(nameFilter.ToLower()));

        return await query
            .OrderBy(e => e.Name)
            .Select(e => new ExerciseCatalogItemDto(e.Id, e.Name, e.Description, e.MediaUrl, e.IsCustom))
            .ToListAsync(ct);
    }
}