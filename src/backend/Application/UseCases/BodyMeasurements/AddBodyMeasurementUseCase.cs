using GymFlow.Application.DTOs.BodyMeasurements;
using GymFlow.Application.Common;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;
using GymFlow.Domain.Enums;

namespace GymFlow.Application.UseCases.BodyMeasurements;

/// <summary>
/// Use case for adding a new body measurement.
/// </summary>
public class AddBodyMeasurementUseCase
{
    private readonly IBodyMeasurementRepository _bodyMeasurementRepository;
    private readonly IMemberRepository _memberRepository;

    public AddBodyMeasurementUseCase(
        IBodyMeasurementRepository bodyMeasurementRepository,
        IMemberRepository memberRepository)
    {
        _bodyMeasurementRepository = bodyMeasurementRepository;
        _memberRepository = memberRepository;
    }

    /// <summary>
    /// Executes the use case to add a new body measurement.
    /// </summary>
    public async Task<Result<BodyMeasurementDto>> ExecuteAsync(
        Guid memberId,
        Guid callerId,
        UserRole callerRole,
        AddBodyMeasurementRequest request,
        CancellationToken ct)
    {
        // Check if the member exists
        var member = await _memberRepository.GetByIdAsync(memberId, ct);
        if (member is null)
            return Result<BodyMeasurementDto>.NotFound("Member not found.");

        // Check ownership and roles
        if (callerRole == UserRole.Member && callerId != memberId)
            return Result<BodyMeasurementDto>.Forbidden("Members can only add measurements for themselves.");

        // Check for idempotency
        var existing = await _bodyMeasurementRepository.GetByClientGuidAsync(request.ClientGuid, ct);
        if (existing is not null)
        {
            var existingDto = MapToDto(existing);
            return Result<BodyMeasurementDto>.Success(existingDto);
        }

        // Create the measurement
        var measurement = BodyMeasurement.Create(
            memberId,
            callerId,
            request.RecordedAt,
            request.WeightKg,
            request.BodyFatPct,
            request.ChestCm,
            request.WaistCm,
            request.HipCm,
            request.ArmCm,
            request.LegCm,
            request.UnitSystem,
            request.ClientGuid,
            request.Notes
        );

        await _bodyMeasurementRepository.AddAsync(measurement, ct);

        return Result<BodyMeasurementDto>.Success(MapToDto(measurement));
    }

    private static BodyMeasurementDto MapToDto(BodyMeasurement measurement)
    {
        return new BodyMeasurementDto(
            measurement.Id,
            measurement.MemberId,
            measurement.RecordedById,
            measurement.RecordedAt,
            measurement.WeightKg,
            measurement.BodyFatPct,
            measurement.ChestCm,
            measurement.WaistCm,
            measurement.HipCm,
            measurement.ArmCm,
            measurement.LegCm,
            measurement.UnitSystem,
            measurement.Notes,
            measurement.ClientGuid
        );
    }
}