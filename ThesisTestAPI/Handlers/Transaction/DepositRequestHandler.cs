using MediatR;
using Microsoft.AspNetCore.Mvc;
using ThesisTestAPI.Models.Transaction;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Transaction
{
    public class DepositRequestHandler : IRequestHandler<DepositRequest, (ProblemDetails?, TransactionResponse?)>
    {
        private readonly WalletTransactionService _service;
        public DepositRequestHandler(WalletTransactionService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, TransactionResponse?)> Handle(DepositRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.Deposit(request);
            return result;
        }
    }
}
