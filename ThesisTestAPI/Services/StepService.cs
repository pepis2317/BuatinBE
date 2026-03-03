using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Enum;
using ThesisTestAPI.Models.Midtrans;
using ThesisTestAPI.Models.Steps;

namespace ThesisTestAPI.Services;

public class StepService
{
    private readonly ThesisDbContext _db;
    private readonly NotificationService _notificationService;
    private readonly BlobStorageService _blobStorageService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly MidtransService _midtransService;

    public StepService(ThesisDbContext db, NotificationService notificationService,
        BlobStorageService blobStorageService, IHttpContextAccessor httpContextAccessor,
        MidtransService midtransService)
    {
        _db = db;
        _notificationService = notificationService;
        _blobStorageService = blobStorageService;
        _httpContextAccessor = httpContextAccessor;
        _midtransService = midtransService;
    }

    private ProblemDetails ProblemDetailTemplate(string detail)
    {
        return new ProblemDetails
        {
            Type = "http://veryCoolAPI.com/errors/invalid-data",
            Title = "Edit step error",
            Detail = detail,
            Instance = _httpContextAccessor.HttpContext?.Request.Path
        };
    }

    public async Task<PaginatedStepsResponse> GetSteps(GetStepsRequest request)
    {
        var query = _db.Steps
            .Where(s => s.ProcessId == request.ProcessId)
            .OrderBy(s => s.CreatedAt);

        var total = await query.CountAsync();

        var steps = await query
            .Skip((request.pageNumber - 1) * request.pageSize)
            .Take(request.pageSize)
            .Select(s => new
            {
                s.StepId,
                s.Title,
                s.Description,
                s.TransactionId,
                s.MinCompleteEstimate,
                s.MaxCompleteEstimate,
                s.Status,
                s.Amount,
                s.CreatedAt,
                s.UpdatedAt,
                Materials = s.Materials
                    .OrderBy(m => m.CreatedAt)
                    .Select(m => new MaterialModel
                    {
                        MaterialId = m.MaterialId,
                        Cost = m.Cost,
                        Name = m.Name,
                        Quantity = m.Quantity,
                        Supplier = m.Supplier,
                        UnitOfMeasurement = m.UnitOfMeasurement,
                        CreatedAt = m.CreatedAt,
                        UpdatedAt = m.UpdatedAt
                    })
                    .ToList()
            })
            .ToListAsync();

        var result = steps.Select(s => new StepResponse
        {
            StepId = s.StepId,
            Title = s.Title,
            Description = s.Description,
            TransactionId = s.TransactionId?.ToString(),
            MinCompleteEstimate = s.MinCompleteEstimate.ToString("dd/MM/yyyy"),
            MaxCompleteEstimate = s.MaxCompleteEstimate.ToString("dd/MM/yyyy"),
            Status = s.Status,
            Price = s.Amount,
            Materials = s.Materials,
            CreatedAt = s.CreatedAt.ToString("dd/MM/yyyy"),
            UpdatedAt = s.UpdatedAt?.ToString("dd/MM/yyyy")
        }).ToList();

        return new PaginatedStepsResponse
        {
            Total = total,
            Steps = result
        };
    }

    public async Task<StepResponse> GetStep(GetStepRequest request)
    {
        var step = await _db.Steps.Where(q => q.StepId == request.StepId).Select(q => new StepResponse
        {
            StepId = q.StepId,
            Title = q.Title,
            Description = q.Description,
            TransactionId = q.TransactionId == null ? null : q.TransactionId.ToString(),
            MinCompleteEstimate = q.MinCompleteEstimate.ToString("dd/MM/yyyy"),
            MaxCompleteEstimate = q.MaxCompleteEstimate.ToString("dd/MM/yyyy"),
            Price = q.Amount,
            Status = q.Status,
        }).FirstOrDefaultAsync();
        return step;
    }

    public async Task<(ProblemDetails?, StepResponse?)> EditStep(EditStepRequest request)
    {
        var step = await _db.Steps.Include(q => q.Transaction).Include(q => q.Process)
            .Where(q => q.StepId == request.StepId).FirstOrDefaultAsync();
        if (step == null)
        {
            return (ProblemDetailTemplate("Invalid step id"), null);
        }

        if (!string.IsNullOrEmpty(request.Title))
        {
            step.Title = request.Title;
        }

        if (!string.IsNullOrEmpty(request.Description))
        {
            step.Description = request.Description;
        }

        if (!string.IsNullOrEmpty(request.Status))
        {
            if (request.Status != StepStatuses.COMPLETED && request.Status != StepStatuses.CANCELLED)
            {
                return (ProblemDetailTemplate("Invalid status update"), null);
            }

            if (request.Status == StepStatuses.CANCELLED && step.Status == StepStatuses.WORKING)
            {
                var buyerId = await _db.Contents.Where(q => q.ContentId == step.Process.RequestId)
                    .Select(q => q.AuthorId).FirstOrDefaultAsync();
                var buyerWallet = await _db.Wallets.Where(q => q.UserId == buyerId).FirstOrDefaultAsync();
                if (buyerWallet == null)
                {
                    return (ProblemDetailTemplate("Buyer wallet doesn't exist"), null);
                }

                MidtransStatus? midtransStatus = null;
                long amountReturnedByWallet = 0;
                if (step.Transaction != null)
                {
                    if (step.Transaction.ExternalRef != null)
                    {
                        var status = await _midtransService.GetStatusAsync(step.Transaction.ExternalRef);
                        if (status != null && status.status_code != "404")
                        {
                            midtransStatus = status;
                        }
                    }
                    else
                    {
                        amountReturnedByWallet += step.Transaction.AmountMinor;
                    }
                }

                if (amountReturnedByWallet > 0)
                {
                    var walletTransaction = new WalletTransaction
                    {
                        TransactionId = Guid.NewGuid(),
                        WalletId = buyerWallet.WalletId,
                        AmountMinor = amountReturnedByWallet,
                        CreatedAt = DateTimeOffset.Now,
                        IdempotencyKey = Guid.NewGuid().ToString(),
                        Type = "Refund",
                        Status = TransactionStatuses.POSTED,
                        PostedAt = DateTime.Now,
                        Direction = "C",
                        SignedAmount = amountReturnedByWallet,
                        ReferenceType = "Wallet",
                        Memo = "Process refund via wallet"
                    };
                    buyerWallet.BalanceMinor += amountReturnedByWallet;
                    _db.WalletTransactions.Add(walletTransaction);
                }

                if (midtransStatus != null)
                {
                    var grossAmount = (long)Convert.ToDouble(midtransStatus.gross_amount);
                    await _midtransService.CreateMidtransRefundAsync(midtransStatus.transaction_id, grossAmount,
                        "Step was cancelled by seller");
                    var snapTransaction = new WalletTransaction
                    {
                        TransactionId = Guid.NewGuid(),
                        WalletId = buyerWallet.WalletId,
                        AmountMinor = grossAmount,
                        Direction = "C",
                        SignedAmount = grossAmount,
                        Type = "Refund",
                        Status = TransactionStatuses.PENDING,
                        CreatedAt = DateTimeOffset.Now,
                        IdempotencyKey = Guid.NewGuid().ToString(),
                        ExternalRef = midtransStatus.order_id,
                        ReferenceType = "MidtransRefund",
                        Memo = "Refund via Midtrans",
                        //ReferenceId = refundRequest.RefundRequestId
                    };
                    _db.WalletTransactions.Add(snapTransaction);
                }

                step.Transaction = null;
            }

            step.Status = request.Status;
        }

        if (request.MinCompleteEstimate != null)
        {
            step.MinCompleteEstimate = (DateTimeOffset)request.MinCompleteEstimate;
        }

        if (request.MaxCompleteEstimate != null)
        {
            step.MaxCompleteEstimate = (DateTimeOffset)request.MaxCompleteEstimate;
        }

        if (request.Images != null)
        {
            var created = new List<ThesisTestAPI.Entities.Image>();
            foreach (var file in request.Images.Where(q => q.Length > 0))
            {
                var contentType = file.ContentType;
                await using var stream = file.OpenReadStream();
                created.Add(new ThesisTestAPI.Entities.Image
                {
                    ImageId = Guid.NewGuid(),
                    ContentId = step.StepId,
                    ImageName = file.FileName,
                    CreatedAt = DateTimeOffset.Now,
                });
                await _blobStorageService.UploadImageFreeAspectAsync(stream, file.FileName, contentType,
                    Enum.BlobContainers.IMAGES);
            }

            _db.Images.AddRange(created);
        }

        step.UpdatedAt = DateTimeOffset.Now;
        await _db.SaveChangesAsync();
        return (null, new StepResponse
        {
            StepId = request.StepId,
        });
    }

    public async Task<StepResponse> DeclineStep(DeclineStepRequest request)
    {
        var step = await _db.Steps.Include(q => q.Process).ThenInclude(q => q.Request).ThenInclude(q => q.Seller)
            .Where(q => q.StepId == request.StepId).FirstOrDefaultAsync();
        step.Status = StepStatuses.DECLINED;
        await _db.SaveChangesAsync();
        var receiverId = step.Process.Request.Seller.OwnerId;
        await _notificationService.SendNotification("Step has been declined", receiverId);
        return new StepResponse
        {
            StepId = step.StepId
        };
    }

    public async Task<StepResponse> CreateStep(CreateStepRequest request)
    {
        var contentId = Guid.NewGuid();
        var content = new Content
        {
            ContentId = contentId,
            AuthorId = request.AuthorId,
            CreatedAt = DateTimeOffset.Now
        };
        var step = new ThesisTestAPI.Entities.Step
        {
            StepId = contentId,
            ProcessId = request.ProcessId,
            Title = request.Title,
            Description = request.Description,
            MinCompleteEstimate = request.MinCompleteEstimate,
            MaxCompleteEstimate = request.MaxCompleteEstimate,
            Amount = request.Amount,
            Status = StepStatuses.SUBMITTED,
            CreatedAt = DateTimeOffset.Now,
            StepNavigation = content
        };
        if (request.PreviousStepId != null)
        {
            var prev = await _db.Steps.Where(q => q.StepId == request.PreviousStepId).FirstOrDefaultAsync();
            if (prev != null)
            {
                prev.NextStepId = contentId;
            }
        }

        _db.Steps.Add(step);
        var process = await _db.Processes.Include(q => q.Request).ThenInclude(q => q.RequestNavigation)
            .Where(q => q.ProcessId == request.ProcessId).FirstOrDefaultAsync();
        if (process != null && process.Status == ProcessStatuses.CREATED)
        {
            process.Status = ProcessStatuses.INPROGRESS;
        }

        var materialsList = new List<Material>();
        foreach (var material in request.Materials)
        {
            materialsList.Add(new Material
            {
                MaterialId = Guid.NewGuid(),
                StepId = contentId,
                Name = material.Name,
                Quantity = material.Quantity,
                UnitOfMeasurement = material.UnitOfMeasurement,
                Supplier = material.Supplier,
                Cost = material.Cost,
                CreatedAt = DateTimeOffset.Now
            });
        }

        _db.Materials.AddRange(materialsList);
        await _db.SaveChangesAsync();
        var receiverId = process.Request.RequestNavigation.AuthorId;
        await _notificationService.SendNotification("Step has been added", receiverId);
        return new StepResponse
        {
            StepId = contentId
        };
    }
}