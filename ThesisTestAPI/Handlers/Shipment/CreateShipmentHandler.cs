using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Enum;
using ThesisTestAPI.Models.Shipment;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Shipment
{
    public class CreateShipmentHandler : IRequestHandler<CreateShipmentRequest, (ProblemDetails?, ShipmentResponse?)>
    {
        private readonly ShipmentService _shipmentService;
        public CreateShipmentHandler(ShipmentService shipmentService)
        {
            _shipmentService = shipmentService;
        }
        public async Task<(ProblemDetails?, ShipmentResponse?)> Handle(CreateShipmentRequest request, CancellationToken cancellationToken)
        {
            var result = await _shipmentService.CreateShipment(request);
            return result;
        }
    }
}
