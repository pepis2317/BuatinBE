using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Review;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Review
{
    public class GetSellerReviewsHandler : IRequestHandler<GetSellerReviewsRequest, (ProblemDetails?, PaginatedReviewsResponse?)>
    {
        private readonly ReviewService _service;
        public GetSellerReviewsHandler(ReviewService service)
        {
            _service = service;
        }

        public async Task<(ProblemDetails?, PaginatedReviewsResponse?)> Handle(GetSellerReviewsRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.GetSellerReviews(request);
            return (null, result);
        }
    }
}
