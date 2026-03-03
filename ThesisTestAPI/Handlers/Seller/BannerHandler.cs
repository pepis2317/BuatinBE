using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Producer;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Producer
{
    public class BannerHandler : IRequestHandler<BannerRequest, (ProblemDetails?, string?)>
    {
        private readonly SellerService _service;
        public BannerHandler(SellerService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, string?)> Handle(BannerRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.HandleBanner(request);
            return (null, result);
        }
    }
}
