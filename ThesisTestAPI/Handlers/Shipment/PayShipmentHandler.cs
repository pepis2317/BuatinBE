using MediatR;
using Microsoft.AspNetCore.Mvc;
using ThesisTestAPI.Models.Shipment;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Shipment
{
    public class PayShipmentHandler : IRequestHandler<PayShipmentRequest, (ProblemDetails?, ShipmentResponse?)>
    {
        private readonly ShipmentService _service;
        public PayShipmentHandler(ShipmentService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, ShipmentResponse?)> Handle(PayShipmentRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.PayShipment(request);
            return result;
        }
    }
}
