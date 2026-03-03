using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Enum;
using ThesisTestAPI.Models.Shipment;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Shipment
{
    public class SendShipmentHandler : IRequestHandler<SendShipmentRequest, (ProblemDetails?, ShipmentResponse?)>
    {
        private readonly ShipmentService _service;
        public SendShipmentHandler(ShipmentService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, ShipmentResponse?)> Handle(SendShipmentRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.SendShipment(request);
            return result;
        }
    }
}
