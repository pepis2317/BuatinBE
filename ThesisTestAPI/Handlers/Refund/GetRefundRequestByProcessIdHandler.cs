using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Refunds;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Refund
{
    public class GetRefundRequestByProcessIdHandler: IRequestHandler<GetRefundRequestByProcessId, (ProblemDetails?, RefundResponse?)>
    {
        private readonly RefundService _service;
        public GetRefundRequestByProcessIdHandler(RefundService service)
        {
            _service = service;
        }

        public async Task<(ProblemDetails?, RefundResponse?)> Handle(GetRefundRequestByProcessId request, CancellationToken cancellationToken)
        {
            var result = await _service.GetRefundRequestByProcessId(request);
            return (null, result);
        }
    }
}
