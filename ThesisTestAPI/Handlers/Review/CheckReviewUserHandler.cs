using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Review;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Review
{
    public class CheckReviewUserHandler : IRequestHandler<CheckReviewUser, (ProblemDetails?, bool?)>
    {
        private readonly ReviewService _service;
        public CheckReviewUserHandler(ReviewService service)
        {
            _service = service;
        }

        public async Task<(ProblemDetails?, bool?)> Handle(CheckReviewUser request, CancellationToken cancellationToken)
        {
            var result = await _service.CheckReviewUser(request);
            return (null, result);
        }
    }
}
