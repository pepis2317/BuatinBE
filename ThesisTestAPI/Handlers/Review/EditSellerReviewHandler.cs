using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Seller;
using ThesisTestAPI.Models.Review;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Review
{
    public class EditSellerReviewHandler : IRequestHandler<EditSellerReviewRequest, (ProblemDetails?, string?)>
    {
        private readonly ReviewService _service;
        public EditSellerReviewHandler(ReviewService service)
        {
            _service = service;
        }

        public async Task<(ProblemDetails?, string?)> Handle(EditSellerReviewRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.EditSellerReview(request);
            return (null, result);
        }
    }
}
