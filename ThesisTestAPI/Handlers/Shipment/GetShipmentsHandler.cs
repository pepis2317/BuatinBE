using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Shipment;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Shipment
{
    public class GetShipmentsHandler : IRequestHandler<GetShipmentsRequest, (ProblemDetails?, PaginatedShipmentResponse?)>
    {
        private readonly ShipmentService _service;
        public GetShipmentsHandler(ShipmentService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, PaginatedShipmentResponse?)> Handle(GetShipmentsRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.GetShipments(request);
            return (null, result);
        }
    }
}
