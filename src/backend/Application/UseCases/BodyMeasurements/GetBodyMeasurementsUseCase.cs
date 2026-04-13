using GymFlow.Domain.Enums;
using GymFlow.Application.DTOs.BodyMeasurements;
using GymFlow.Application.Common;
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
        var member = await _memberRepository.GetByIdAsync(memberId, ct);
        if (member is null)
            return Result<IReadOnlyList<BodyMeasurementDto>>.NotFound("Member not found.");

        // Check ownership and roles
        if (callerRole == UserRole.Member && callerId != memberId)
            return Result<IReadOnlyList<BodyMeasurementDto>>.Forbidden("Members can only view their own measurements.");

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

        return Result<IReadOnlyList<BodyMeasurementDto>>.Success(dtos.AsReadOnly());
    }
}