using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Review;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Review
{
    public class DeleteSellerReviewHandler : IRequestHandler<DeleteSellerReviewRequest, (ProblemDetails?, string?)>
    {
        private readonly ReviewService _service;
        public DeleteSellerReviewHandler(ReviewService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, string?)> Handle(DeleteSellerReviewRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.DeleteReview(request.ReviewId);
            return (null, result);
        }
    }
}
