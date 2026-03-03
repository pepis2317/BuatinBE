using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Producer;
using ThesisTestAPI.Services;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory.Model;

namespace ThesisTestAPI.Handlers.Seller
{
    public class GetSellerByIdHandler : IRequestHandler<GetSellerRequest, (ProblemDetails?, SellerResponse?)>
    {
        private readonly SellerService _service;
        public GetSellerByIdHandler(SellerService service)
        {
            _service = service;
        }

        public async Task<(ProblemDetails?, SellerResponse?)> Handle(GetSellerRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.GetSellerById(request);
            return result;
        }
    }
}
