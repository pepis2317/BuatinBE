using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Producer;
using ThesisTestAPI.Models.Seller;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Seller
{
    public class SellersByQueryHandler : IRequestHandler<SellerQuery, (ProblemDetails?, PaginatedSellersResponse?)>
    {
        private readonly IValidator<SellerQuery> _validator;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly SellerService _service;
        public SellersByQueryHandler(IHttpContextAccessor httpContextAccessor, IValidator<SellerQuery> validator, SellerService service)
        {
            _httpContextAccessor = httpContextAccessor;
            _validator = validator;
            _service = service;
        }

        public async Task<(ProblemDetails?, PaginatedSellersResponse?)> Handle(SellerQuery request, CancellationToken cancellationToken)
        {
            var validation = await _validator.ValidateAsync(request);
            if (!validation.IsValid)
            {
                var problemDetails = new ProblemDetails
                {
                    Type = "http://veryCoolAPI.com/errors/invalid-data",
                    Title = "Invalid Request Data",
                    Detail = string.Join("; ", validation.Errors.Select(e => e.ErrorMessage)),
                    Instance = _httpContextAccessor.HttpContext?.Request.Path
                };
                return (problemDetails, null);
            }

            var result = await _service.GetSellersByQuery(request);
            return (null, result);

        }
    }
}
