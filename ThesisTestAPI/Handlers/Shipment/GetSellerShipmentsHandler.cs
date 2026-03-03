using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Process;
using ThesisTestAPI.Models.Shipment;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Shipment
{
    public class GetSellerShipmentsHandler : IRequestHandler<GetSellerShipmentsRequest, (ProblemDetails?, PaginatedShipmentResponse?)>
    {
        private readonly ShipmentService _service;
        public GetSellerShipmentsHandler(ShipmentService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, PaginatedShipmentResponse?)> Handle(GetSellerShipmentsRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.GetSellerShipments(request);
            return (null, result);
        }
    }
}
