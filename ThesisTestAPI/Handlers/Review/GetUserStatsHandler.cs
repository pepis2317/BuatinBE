using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Review;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Review
{
    public class GetUserStatsHandler : IRequestHandler<GetUserStatsRequest, (ProblemDetails?, UserStatsResponse?)>
    {
        private readonly ReviewService _service;
        public GetUserStatsHandler(ReviewService service)
        {
            _service = service;
        }

        public async Task<(ProblemDetails?, UserStatsResponse?)> Handle(GetUserStatsRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.GetUserStats(request);
            return (null, result);
        }
    }
}
