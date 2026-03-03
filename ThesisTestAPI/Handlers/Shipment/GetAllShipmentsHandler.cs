using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Shipment;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Shipment
{
    public class GetAllShipmentsHandler : IRequestHandler<GetAllShipmentsRequest, (ProblemDetails?, PaginatedShipmentResponse?)>
    {
        private readonly ShipmentService _service;
        public GetAllShipmentsHandler(ShipmentService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, PaginatedShipmentResponse?)> Handle(GetAllShipmentsRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.GetAllShipments(request);
            return (null, result);
        }
    }
}
