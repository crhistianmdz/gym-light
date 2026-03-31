using GymFlow.Application.DTOs.Metrics;
using GymFlow.Domain.Entities;
using GymFlow.Domain.Interfaces;

namespace GymFlow.Application.UseCases.Admin;

public class RegisterPaymentUseCase
{
    private readonly IPaymentRepository _paymentRepository;
    private readonly IMemberRepository _memberRepository;

    public RegisterPaymentUseCase(IPaymentRepository paymentRepository, IMemberRepository memberRepository)
    {
        _paymentRepository = paymentRepository;
        _memberRepository = memberRepository;
    }

    public async Task<Result<PaymentDto>> ExecuteAsync(RegisterPaymentRequest req, Guid actingUserId, CancellationToken ct = default)
    {
        // Validate Amount
        if (req.Amount <= 0)
        {
            return Result<PaymentDto>.ValidationError("El monto debe ser mayor a cero.");
        }

        // Idempotency Check
        if (await _paymentRepository.ClientGuidExistsAsync(req.ClientGuid, ct))
        {
            var existingPayment = await _paymentRepository.GetByClientGuidAsync(req.ClientGuid, ct);
            return Result<PaymentDto>.Success(existingPayment!.ToDto());
        }

        // Validate Member existence, when MemberId is provided
        if (req.MemberId.HasValue && 
            await _memberRepository.GetByIdAsync(req.MemberId.Value, ct) == null)
        {
            return Result<PaymentDto>.ValidationError("El socio proporcionado no existe.");
        }

        // Create Payment Entity
        var payment = Payment.Create(
            req.MemberId,
            req.Amount,
            Enum.Parse<PaymentCategory>(req.Category),
            actingUserId,
            req.ClientGuid,
            req.Notes,
            req.SaleId
        );

        // Add to repository
        await _paymentRepository.AddAsync(payment, ct);
        return Result<PaymentDto>.Success(payment.ToDto());
    }
}