using GymFlow.Domain.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GymFlow.WebAPI.Filters;

/// <summary>
/// Filtro de idempotencia para endpoints que reciben transacciones offline.
/// Lee X-Client-Guid del header de la request.
/// Si el GUID ya existe en AccessLogs → cortocircuita y retorna 200 OK.
/// Si no existe → deja pasar al handler (el Use Case lo registrará).
///
/// RFC §4: "Si un ClientGuid ya existe, el servidor responderá 200 OK
/// pero no procesará la lógica de nuevo."
/// </summary>
public class IdempotencyFilter : IAsyncActionFilter
{
    private const string ClientGuidHeader = "X-Client-Guid";
    private readonly IAccessLogRepository _accessLogs;
        private readonly ISaleRepository _saleRepository;

    public IdempotencyFilter(IAccessLogRepository accessLogs, ISaleRepository saleRepository)
        {
            _accessLogs = accessLogs;
            _saleRepository = saleRepository;
        }
    {
        _accessLogs = accessLogs;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(ClientGuidHeader, out var rawGuid))
        {
            // Sin header: el body ya contiene ClientGuid, el Use Case maneja la idempotencia
            await next();
            return;
        }

        if (!Guid.TryParse(rawGuid, out var clientGuid))
        {
            context.Result = new BadRequestObjectResult(new ProblemDetails
            {
                Title = $"El header {ClientGuidHeader} no es un GUID válido.",
                Status = 400
            });
            return;
        }

        var alreadyProcessed = await __saleRepository.ClientGuidExistsAsync(clientGuid);
        if (alreadyProcessed)
        {
            // Duplicado detectado: responder 200 OK sin reejecutar
            context.Result = new OkObjectResult(new
            {
                Message = "Transacción ya procesada.",
                ClientGuid = clientGuid
            });
            return;
        }

        await next();
    }
}
