using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Refunds;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Refund
{
    public class GetRefundRequestsHandler : IRequestHandler<GetRefundRequest, (ProblemDetails?, PaginatedRefundRequestResponse?)>
    {
        private readonly RefundService _service;
        public GetRefundRequestsHandler(RefundService service)
        {
            _service = service;
        }

        public async Task<(ProblemDetails?, PaginatedRefundRequestResponse?)> Handle(GetRefundRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.GetRefundRequests(request);
            return (null, result);
        }
    }
}
