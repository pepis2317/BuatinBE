using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Producer;
using ThesisTestAPI.Models.Seller;
using ThesisTestAPI.Services;

namespace ThesisTestAPI.Handlers.Seller
{
    public class SellerPictureHandler : IRequestHandler<UploadSellerPictureRequest, (ProblemDetails?, string?)>
    {
        private readonly SellerService _service;
        public SellerPictureHandler(SellerService service)
        {
            _service = service;
        }
        public async Task<(ProblemDetails?, string?)> Handle(UploadSellerPictureRequest request, CancellationToken cancellationToken)
        {
            var result = await _service.HandleSellerPicture(request);
            return (null, result);

        }
    }
}
