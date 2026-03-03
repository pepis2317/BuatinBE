using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Steps;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Steps
{
    public class GetStepHandler : IRequestHandler<GetStepRequest, (ProblemDetails?, StepResponse?)>
    {
        private readonly StepService _stepService;
        public GetStepHandler(StepService stepService)
        {
            _stepService = stepService;
        }

        public async Task<(ProblemDetails?, StepResponse?)> Handle(GetStepRequest request, CancellationToken cancellationToken)
        {
            var result = await _stepService.GetStep(request);
            return (null, result);
        }
    }
}
