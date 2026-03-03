using MediatR;
using Microsoft.AspNetCore.Mvc;
using ThesisTestAPI.Models.Biteship;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Biteship
{
    public class CreateShipmentByCoordinatesHandler : IRequestHandler<CreateShipmentByCoordinatesRequest, (ProblemDetails?, OrderCreatedResponse?)>
    {
        private readonly ShipmentService _shipmentService;
        public CreateShipmentByCoordinatesHandler(ShipmentService shipmentService)
        {
            _shipmentService = shipmentService;
        }
        public async Task<(ProblemDetails?, OrderCreatedResponse?)> Handle(CreateShipmentByCoordinatesRequest request, CancellationToken cancellationToken)
        {
            var result = await _shipmentService.CreateShipmentByCoordinates(request);
            return result;
        }
    }
}
