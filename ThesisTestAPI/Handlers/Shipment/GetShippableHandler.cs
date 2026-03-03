using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Enum;
using ThesisTestAPI.Models.Process;
using ThesisTestAPI.Models.Shipment;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Shipment
{
    public class GetShippableHandler : IRequestHandler<GetShippableRequest, (ProblemDetails?, PaginatedProcessesResponse?)>
    {
        private readonly ShipmentService _service;
        public GetShippableHandler(ShipmentService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, PaginatedProcessesResponse?)> Handle(GetShippableRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.GetShippable(request);
            return (null, result);
        }
    }
}
