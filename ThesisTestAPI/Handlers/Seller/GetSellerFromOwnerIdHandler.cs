using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Producer;
using ThesisTestAPI.Models.Seller;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Seller
{
    public class GetSellerFromOwnerIdHandler : IRequestHandler<GetSellerFromOwnerIdRequest, (ProblemDetails?, SellerResponse?)>
    {
        private readonly SellerService _service;
        public GetSellerFromOwnerIdHandler( SellerService service)
        {
            _service = service;
        }

        public async Task<(ProblemDetails?, SellerResponse?)> Handle(GetSellerFromOwnerIdRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.GetSellerFromOwnerId(request);
            return (null, result);
        }
    }
}
