using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Review;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Review
{
    public class EditUserReviewHandler : IRequestHandler<EditUserReviewRequest, (ProblemDetails?, string?)>
    {
        private readonly ReviewService _service;
        public EditUserReviewHandler(ReviewService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, string?)> Handle(EditUserReviewRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.EditUserReview(request);
            return (null, result);
        }
    }
}
