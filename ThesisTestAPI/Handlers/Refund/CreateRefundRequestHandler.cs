using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Enum;
using ThesisTestAPI.Models.Refunds;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Refund
{
    public class CreateRefundRequestHandler : IRequestHandler<CreateRefundRequest, (ProblemDetails?, RefundResponse?)>
    {
        private readonly RefundService _service;
        public CreateRefundRequestHandler(RefundService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, RefundResponse?)> Handle(CreateRefundRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateRefund(request);
            return (null, result);
        }
    }
}
