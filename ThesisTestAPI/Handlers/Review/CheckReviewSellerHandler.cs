using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Review;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Review
{
    public class CheckReviewSellerHandler : IRequestHandler<CheckReviewSeller, (ProblemDetails?, bool?)>
    {
        private readonly ReviewService _service;
        public CheckReviewSellerHandler(ReviewService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, bool?)> Handle(CheckReviewSeller request, CancellationToken cancellationToken)
        {
            var result = await _service.CheckReviewSeller(request);
            return (null, result);
        }
    }
}
