using MediatR;
using Microsoft.AspNetCore.Mvc;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Review;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Review
{
    public class CreateSellerReviewHandler : IRequestHandler<CreateSellerReviewRequest, (ProblemDetails?, string?)>
    {
        private readonly ReviewService _reviewService;
        public CreateSellerReviewHandler(ReviewService reviewService)
        {
            _reviewService = reviewService;
        }
        public async Task<(ProblemDetails?, string?)> Handle(CreateSellerReviewRequest request, CancellationToken cancellationToken)
        {
            var result = await _reviewService.CreateReview(request.AuthorId,request.Review, request.SellerId,null,request.Rating);
            return (null, result);
        }
    }
}
