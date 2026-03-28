namespace GymFlow.Application.DTOs;

public record CreateSaleRequestDto(
    Guid ClientGuid,
    List<SaleLineRequestDto> Lines,
    Guid PerformedByUserId
);