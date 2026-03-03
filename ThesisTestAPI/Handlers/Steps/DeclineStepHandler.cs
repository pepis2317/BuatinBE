using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Enum;
using ThesisTestAPI.Models.Steps;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Steps
{
    public class DeclineStepHandler : IRequestHandler<DeclineStepRequest, (ProblemDetails?, StepResponse?)>
    {
        private readonly StepService _service;
        public DeclineStepHandler(StepService service)
        {
            _service = service;
        }  
        public async Task<(ProblemDetails?, StepResponse?)> Handle(DeclineStepRequest request, CancellationToken cancellationToken)
        {
            var  result = await _service.DeclineStep(request);
            return (null, result);
        }
    }
}
