namespace GymFlow.Domain.Interfaces;

using GymFlow.Domain.Entities;

public interface IUserRepository
{
    Task<AppUser?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<AppUser?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(AppUser user, CancellationToken ct = default);
}