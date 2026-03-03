using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Shipment;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Shipment
{
    public class GetShipmentHandler : IRequestHandler<GetShipmentRequest, (ProblemDetails?, ShipmentResponse?)>
    {
        private readonly ShipmentService _service;
        public GetShipmentHandler(ShipmentService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, ShipmentResponse?)> Handle(GetShipmentRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.GetShipment(request);
            return (null, result);
        }
    }
}
