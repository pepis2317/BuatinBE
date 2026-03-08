using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Enum;
using ThesisTestAPI.Models.Refunds;

namespace ThesisTestAPI.Services;

public class RefundService
{
    private readonly ThesisDbContext _db;
    private readonly BlobStorageService _blobStorageService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public RefundService(ThesisDbContext db, BlobStorageService blobStorageService,
        IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _blobStorageService = blobStorageService;
        _httpContextAccessor = httpContextAccessor;
    }

    private ProblemDetails ProblemDetailTemplate(string detail)
    {
        return new ProblemDetails
        {
            Type = "http://veryCoolAPI.com/errors/invalid-data",
            Title = "Respond refund request error",
            Detail = detail,
            Instance = _httpContextAccessor.HttpContext?.Request.Path
        };
    }

    public async Task<(ProblemDetails?, RefundResponse?)> RespondRefund(RespondRefundRequest request)
    {
        if (request.Answer != RequestStatuses.DECLINED && request.Answer != RequestStatuses.ACCEPTED)
        {
            return (ProblemDetailTemplate("Answer must be 'Declined' or 'Accepted'"), null);
        }

        var refundRequest = await _db.RefundRequests.Include(q => q.Process).Include(q => q.RefundRequestNavigation)
            .Where(q => q.RefundRequestId == request.RefundRequestId && q.Status == RequestStatuses.PENDING)
            .FirstOrDefaultAsync();
        if (refundRequest == null)
        {
            return (ProblemDetailTemplate("Refund request doesn't exist"), null);
        }

        if (request.Answer == RequestStatuses.DECLINED)
        {
            refundRequest.Status = RequestStatuses.DECLINED;
            await _db.SaveChangesAsync();
            return (null, new RefundResponse { RefundId = refundRequest.RefundRequestId });
        }

        //get amount by querying the total price of the process
        long amountReturnedBySnap = 0;
        long amountReturnedByWallet = 0;
        var steps = await _db.Steps.Include(q => q.Transaction).Where(q =>
            q.ProcessId == refundRequest.ProcessId &&
            (q.Status == StepStatuses.COMPLETED || q.Status == StepStatuses.WORKING)).ToListAsync();
        foreach (var step in steps)
        {
            if (step.Transaction != null)
            {
                if (step.Transaction.ExternalRef != null)
                {
                    amountReturnedBySnap += step.Transaction.AmountMinor;
                }
                else
                {
                    amountReturnedByWallet += step.Transaction.AmountMinor;
                }
            }
        }

        var buyerWallet = await _db.Wallets.Where(q => q.UserId == refundRequest.RefundRequestNavigation.AuthorId)
            .FirstOrDefaultAsync();
        if (buyerWallet == null)
        {
            return (ProblemDetailTemplate("Buyer wallet doesn't exist"), null);
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

        if (amountReturnedBySnap > 0)
        {
            var snapTransaction = new WalletTransaction()
            {
                TransactionId = Guid.NewGuid(),
                WalletId = buyerWallet.WalletId,
                AmountMinor = amountReturnedBySnap,
                CreatedAt = DateTimeOffset.Now,
                IdempotencyKey = Guid.NewGuid().ToString(),
                Type = "Refund",
                Status = TransactionStatuses.POSTED,
                PostedAt = DateTime.Now,
                Direction = "C",
                SignedAmount = amountReturnedBySnap,
                ReferenceType = "Wallet",
                Memo = "Process refund via Snap"
            };
            buyerWallet.BalanceMinor += amountReturnedBySnap;
            _db.WalletTransactions.Add(snapTransaction);
        }

        var finalStep = steps.Where(q => q.ProcessId == refundRequest.ProcessId).OrderByDescending(q => q.CreatedAt)
            .FirstOrDefault();
        refundRequest.Status = RequestStatuses.ACCEPTED;
        refundRequest.UpdatedAt = DateTimeOffset.Now;
        finalStep.Status = StepStatuses.CANCELLED;
        finalStep.UpdatedAt = DateTimeOffset.Now;
        refundRequest.Process.Status = ProcessStatuses.CANCELLED;
        refundRequest.Process.UpdatedAt = DateTimeOffset.Now;
        await _db.SaveChangesAsync();
        return (null, new RefundResponse { RefundId = refundRequest.RefundRequestId });
    }

    public async Task<PaginatedRefundRequestResponse> GetRefundRequests(GetRefundRequest request)
    {
        var refunds = await _db.RefundRequests
            .Include(q => q.Process).ThenInclude(q => q.Request).ThenInclude(q => q.RequestNavigation)
            .ThenInclude(q => q.Author)
            .Include(q => q.Process).ThenInclude(q => q.Request).ThenInclude(q => q.Seller)
            .Skip((request.pageNumber - 1) * request.pageSize).OrderByDescending(q => q.CreatedAt).ToListAsync();
        var list = new List<RefundResponse>();
        foreach (var refund in refunds)
        {
            var pfp = "";
            var sellerPic = "";
            var seller = refund.Process.Request.Seller;
            var user = refund.Process.Request.RequestNavigation.Author;
            if (!string.IsNullOrEmpty(user.Pfp))
            {
                pfp = await _blobStorageService.GetTemporaryImageUrl(user.Pfp, Enum.BlobContainers.PFP);
            }

            if (!string.IsNullOrEmpty(seller.SellerPicture))
            {
                sellerPic = await _blobStorageService.GetTemporaryImageUrl(seller.SellerPicture,
                    Enum.BlobContainers.SELLERPICTURE);
            }

            list.Add(new RefundResponse
            {
                RefundId = refund.RefundRequestId,
                ProcessId = refund.ProcessId,
                Message = refund.Message,
                Status = refund.Status,
                Seller = new Models.Producer.SellerResponse
                {
                    SellerId = seller.SellerId,
                    SellerName = seller.SellerName,
                    SellerPicture = sellerPic,
                },
                User = new Models.User.UserResponse
                {
                    UserId = user.UserId,
                    UserName = user.UserName,
                    Pfp = pfp
                }
            });
        }

        var total = await _db.RefundRequests.CountAsync();
        return new PaginatedRefundRequestResponse
        {
            RefundRequests = list,
            Total = total
        };
    }

    public async Task<RefundResponse> GetRefundRequestByProcessId(GetRefundRequestByProcessId request)
    {
        var refund = await _db.RefundRequests
            .Include(q => q.Process).ThenInclude(q => q.Request).ThenInclude(q => q.RequestNavigation)
            .ThenInclude(q => q.Author)
            .Include(q => q.Process).ThenInclude(q => q.Request).ThenInclude(q => q.Seller)
            .Where(q => q.ProcessId == request.ProcessId).FirstOrDefaultAsync();
        var pfp = "";
        var sellerPic = "";
        var seller = refund.Process.Request.Seller;
        var user = refund.Process.Request.RequestNavigation.Author;
        if (!string.IsNullOrEmpty(user.Pfp))
        {
            pfp = await _blobStorageService.GetTemporaryImageUrl(user.Pfp, Enum.BlobContainers.PFP);
        }

        if (!string.IsNullOrEmpty(seller.SellerPicture))
        {
            sellerPic = await _blobStorageService.GetTemporaryImageUrl(seller.SellerPicture,
                Enum.BlobContainers.SELLERPICTURE);
        }

        return new RefundResponse
        {
            RefundId = refund.RefundRequestId,
            ProcessId = refund.ProcessId,
            Message = refund.Message,
            Status = refund.Status,
            Seller = new Models.Producer.SellerResponse
            {
                SellerId = seller.SellerId,
                SellerName = seller.SellerName,
                SellerPicture = sellerPic,
            },
            User = new Models.User.UserResponse
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Pfp = pfp
            }
        };
    }

    public async Task<RefundResponse> GetRefundRequestById(GetRefundRequestById request)
    {
        var refund = await _db.RefundRequests
            .Include(q => q.Process).ThenInclude(q => q.Request).ThenInclude(q => q.RequestNavigation)
            .ThenInclude(q => q.Author)
            .Include(q => q.Process).ThenInclude(q => q.Request).ThenInclude(q => q.Seller)
            .Where(q => q.RefundRequestId == request.RefundRequestId).FirstOrDefaultAsync();
        var pfp = "";
        var sellerPic = "";
        var seller = refund.Process.Request.Seller;
        var user = refund.Process.Request.RequestNavigation.Author;
        if (!string.IsNullOrEmpty(user.Pfp))
        {
            pfp = await _blobStorageService.GetTemporaryImageUrl(user.Pfp, Enum.BlobContainers.PFP);
        }

        if (!string.IsNullOrEmpty(seller.SellerPicture))
        {
            sellerPic = await _blobStorageService.GetTemporaryImageUrl(seller.SellerPicture,
                Enum.BlobContainers.SELLERPICTURE);
        }

        return new RefundResponse
        {
            RefundId = refund.RefundRequestId,
            ProcessId = refund.ProcessId,
            Message = refund.Message,
            Status = refund.Status,
            Seller = new Models.Producer.SellerResponse
            {
                SellerId = seller.SellerId,
                SellerName = seller.SellerName,
                SellerPicture = sellerPic,
            },
            User = new Models.User.UserResponse
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Pfp = pfp
            }
        };
    }

    public async Task<RefundResponse> CreateRefund(CreateRefundRequest request)
    {
        var sellerId = await _db.Processes.Include(q => q.Request).ThenInclude(q => q.Seller)
            .Where(q => q.ProcessId == request.ProcessId).Select(q => q.Request.Seller.OwnerId).FirstOrDefaultAsync();
        var contentId = Guid.NewGuid();
        var content = new Content
        {
            ContentId = contentId,
            AuthorId = request.AuthorId,
            CreatedAt = DateTimeOffset.Now
        };
        var refundRequest = new ThesisTestAPI.Entities.RefundRequest
        {
            RefundRequestId = contentId,
            Message = request.Message,
            Status = RequestStatuses.PENDING,
            ProcessId = request.ProcessId,
            SellerUserId = sellerId,
            RefundRequestNavigation = content,
            CreatedAt = DateTimeOffset.Now
        };
        _db.RefundRequests.Add(refundRequest);
        await _db.SaveChangesAsync();
        return new RefundResponse { RefundId = contentId };
    }
}