using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Review;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Review
{
    public class GetSellerStatsHandler : IRequestHandler<GetSellerStatsRequest, (ProblemDetails?, SellerStatsResponse?)>
    {
        private readonly ReviewService _service;
        public GetSellerStatsHandler(ReviewService service)
        {
            _service = service;
        }

        public async Task<(ProblemDetails?, SellerStatsResponse?)> Handle(GetSellerStatsRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.GetSellerStats(request);
            return (null, result);
        }
    }
}
