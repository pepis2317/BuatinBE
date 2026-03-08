using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Enum;
using ThesisTestAPI.Models.Midtrans;
using ThesisTestAPI.Models.Refunds;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Refund
{
    public class RespondRefundRequestHandler : IRequestHandler<RespondRefundRequest, (ProblemDetails?, RefundResponse?)>
    {
        private readonly RefundService _service;
        public RespondRefundRequestHandler(RefundService service)
        {
            _service = service;
        }

        public async Task<(ProblemDetails?, RefundResponse?)> Handle(RespondRefundRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.RespondRefund(request);
            return result;
        }
    }
}
