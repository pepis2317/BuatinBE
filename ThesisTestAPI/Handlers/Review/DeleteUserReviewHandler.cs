using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Review;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Review
{
    public class DeleteUserReviewHandler : IRequestHandler<DeleteUserReviewRequest, (ProblemDetails?, string?)>
    {
        private readonly ReviewService _service;
        public DeleteUserReviewHandler(ReviewService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, string?)> Handle(DeleteUserReviewRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.DeleteReview(request.ReviewId);
            return (null, result);
        }
    }
}
