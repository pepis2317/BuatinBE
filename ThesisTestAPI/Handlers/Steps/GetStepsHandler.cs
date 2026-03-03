using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Steps;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Steps
{
    public class GetStepsHandler : IRequestHandler<GetStepsRequest, (ProblemDetails?, PaginatedStepsResponse?)>
    {
        private readonly StepService _stepService;

        public GetStepsHandler(StepService stepService)
        {
            _stepService =  stepService;
        }

        public async Task<(ProblemDetails?, PaginatedStepsResponse?)> Handle(GetStepsRequest request, CancellationToken cancellationToken)
        {
            var result = await _stepService.GetSteps(request);

            return (null, result);
        }
    }
}