using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Transaction;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Transaction
{
    public class GetWalletBalanceHandler: IRequestHandler<GetWalletRequest,(ProblemDetails?,long?)>
    {
        private readonly WalletTransactionService _service;
        public GetWalletBalanceHandler(WalletTransactionService service)
        {
            _service = service;
        }

        public async Task<(ProblemDetails?, long?)> Handle(GetWalletRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.GetWalletBalance(request);
            return (null, result);
        }
    }
}
