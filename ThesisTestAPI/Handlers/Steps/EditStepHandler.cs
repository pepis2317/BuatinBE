using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Drawing.Text;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Enum;
using ThesisTestAPI.Models.Midtrans;
using ThesisTestAPI.Models.Steps;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Steps
{
    public class EditStepHandler : IRequestHandler<EditStepRequest, (ProblemDetails?, StepResponse?)>
    {
        private readonly StepService _stepService;
        public EditStepHandler(StepService stepService)
        {
            _stepService = stepService;
        }
        public async Task<(ProblemDetails?, StepResponse?)> Handle(EditStepRequest request, CancellationToken cancellationToken)
        {
            var result = await _stepService.EditStep(request);
            return result;
        }
    }
}
