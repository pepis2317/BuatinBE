using MediatR;
using Microsoft.AspNetCore.Mvc;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Review;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Review
{
    public class CreateUserReviewHandler : IRequestHandler<CreateUserReviewRequest, (ProblemDetails?, string?)>
    {
        private readonly ReviewService _reviewService;
        public CreateUserReviewHandler(ReviewService reviewService)
        {
            _reviewService = reviewService;
        }
        public async Task<(ProblemDetails?, string?)> Handle(CreateUserReviewRequest request, CancellationToken cancellationToken)
        {
            var result = await _reviewService.CreateReview(request.AuthorId,request.Review, null ,request.UserId,request.Rating);
            return (null, result);
        }
    }
}
