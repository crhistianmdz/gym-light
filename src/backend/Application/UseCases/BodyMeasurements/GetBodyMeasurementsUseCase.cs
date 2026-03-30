using GymFlow.Application.DTOs.BodyMeasurements;
using GymFlow.Application.Results;
using GymFlow.Domain.Interfaces;

namespace GymFlow.Application.UseCases.BodyMeasurements;

/// <summary>
/// Use case for retrieving body measurements of a member.
/// </summary>
public class GetBodyMeasurementsUseCase
{
    private readonly IBodyMeasurementRepository _bodyMeasurementRepository;
    private readonly IMemberRepository _memberRepository;

    public GetBodyMeasurementsUseCase(
        IBodyMeasurementRepository bodyMeasurementRepository,
        IMemberRepository memberRepository)
    {
        _bodyMeasurementRepository = bodyMeasurementRepository;
        _memberRepository = memberRepository;
    }

    /// <summary>
    /// Executes the use case to retrieve all body measurements for a member.
    /// </summary>
    public async Task<Result<IReadOnlyList<BodyMeasurementDto>>> ExecuteAsync(
        Guid memberId,
        Guid callerId,
        UserRole callerRole,
        CancellationToken ct)
    {
        // Check if the member exists
        var memberExists = await _memberRepository.ExistsAsync(memberId, ct);
        if (!memberExists)
            return Result.NotFound<IReadOnlyList<BodyMeasurementDto>>("Member not found.");

        // Check ownership and roles
        if (callerRole == UserRole.Member && callerId != memberId)
            return Result.Forbidden<IReadOnlyList<BodyMeasurementDto>>("Members can only view their own measurements.");

        // Retrieve and return measurements
        var measurements = await _bodyMeasurementRepository.GetByMemberIdAsync(memberId, ct);
        var dtos = measurements.Select(x => new BodyMeasurementDto(
            x.Id,
            x.MemberId,
            x.RecordedById,
            x.RecordedAt,
            x.WeightKg,
            x.BodyFatPct,
            x.ChestCm,
            x.WaistCm,
            x.HipCm,
            x.ArmCm,
            x.LegCm,
            x.UnitSystem,
            x.Notes,
            x.ClientGuid
        )).ToList();

        return Result.Ok(dtos);
    }
}