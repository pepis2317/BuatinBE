using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Review;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Review
{
    public class GetUserReviewsHandler: IRequestHandler<GetUserReviewsRequest, (ProblemDetails?, PaginatedReviewsResponse?)>
    {
        private readonly ReviewService _service;
        public GetUserReviewsHandler(ReviewService service)
        {
            _service = service;
        }

        public async Task<(ProblemDetails?, PaginatedReviewsResponse?)> Handle(GetUserReviewsRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.GetUserReviews(request);
            return (null, result);
        }
    }
}
