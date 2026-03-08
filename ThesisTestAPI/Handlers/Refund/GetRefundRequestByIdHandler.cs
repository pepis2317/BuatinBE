using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Refunds;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Refund
{
    public class GetRefundRequestByIdHandler : IRequestHandler<GetRefundRequestById, (ProblemDetails?, RefundResponse?)>
    {
        private readonly RefundService _service;
        public GetRefundRequestByIdHandler(RefundService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, RefundResponse?)> Handle(GetRefundRequestById request, CancellationToken cancellationToken)
        {
            var result = await _service.GetRefundRequestById(request);
            return (null, result);
        }
    }
}
