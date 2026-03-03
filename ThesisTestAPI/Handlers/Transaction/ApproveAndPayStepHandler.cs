using MediatR;
using Microsoft.AspNetCore.Mvc;
using ThesisTestAPI.Models.Transaction;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Transaction
{
    public class ApproveAndPayStepHandler : IRequestHandler<ApproveAndPayStepRequest, (ProblemDetails?, TransactionResponse?)>
    {
        private readonly WalletTransactionService _service;
        public ApproveAndPayStepHandler(WalletTransactionService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, TransactionResponse?)> Handle(ApproveAndPayStepRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.ApproveAndPayStep(request);
            return result;
        }
    }
}
