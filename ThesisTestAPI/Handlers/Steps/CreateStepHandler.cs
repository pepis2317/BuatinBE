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
    public class CreateStepHandler : IRequestHandler<CreateStepRequest, (ProblemDetails?, StepResponse?)>
    {
        private readonly StepService _service;
        public CreateStepHandler(StepService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, StepResponse?)> Handle(CreateStepRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.CreateStep(request);
            return (null, result);
        }
    }
}
