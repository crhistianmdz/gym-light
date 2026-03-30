using GymFlow.Domain.Entities;

namespace GymFlow.Domain.Interfaces;

/// <summary>
/// Repository contract for BodyMeasurement entity.
/// </summary>
public interface IBodyMeasurementRepository
{
    /// <summary>
    /// Adds a new BodyMeasurement.
    /// </summary>
    /// <param name="measurement">The BodyMeasurement to add.</param>
    /// <param name="ct">Cancellation token.</param>
    Task AddAsync(BodyMeasurement measurement, CancellationToken ct = default);

    /// <summary>
    /// Retrieves all BodyMeasurements of a specific member, ordered by RecordedAt descending.
    /// </summary>
    /// <param name="memberId">The member ID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A list of BodyMeasurements.</returns>
    Task<IReadOnlyList<BodyMeasurement>> GetByMemberIdAsync(Guid memberId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a BodyMeasurement by its client GUID.
    /// </summary>
    /// <param name="clientGuid">The client GUID.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The BodyMeasurement or null if not found.</returns>
    Task<BodyMeasurement?> GetByClientGuidAsync(string clientGuid, CancellationToken ct = default);
}