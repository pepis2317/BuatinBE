using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Enum;
using ThesisTestAPI.Models.Transaction;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Transaction
{
    public class WithdrawHandler : IRequestHandler<WithdrawRequest, (ProblemDetails?, TransactionResponse?)>
    {
        private readonly WalletTransactionService _service;
        public WithdrawHandler(WalletTransactionService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, TransactionResponse?)> Handle(WithdrawRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.Withdraw(request);
            return result;
        }
    }
}
