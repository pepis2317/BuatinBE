using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Enum;
using ThesisTestAPI.Handlers.Transaction;
using ThesisTestAPI.Models.Transaction;

namespace ThesisTestAPI.Services;

public class WalletTransactionService
{
    private readonly ThesisDbContext _db;
    private readonly MidtransService _midtransService;
    private readonly NotificationService _notificationService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WalletTransactionService(ThesisDbContext db, MidtransService midtransService,
        NotificationService notificationService, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _midtransService = midtransService;
        _notificationService = notificationService;
        _httpContextAccessor = httpContextAccessor;
    }

    private ProblemDetails ProblemDetailTemplate(string detail)
    {
        return new ProblemDetails
        {
            Type = "http://veryCoolAPI.com/errors/invalid-data",
            Title = "Midtrans error",
            Detail = detail,
            Instance = _httpContextAccessor.HttpContext?.Request.Path
        };
    }
    
    public async Task<(ProblemDetails?, TransactionResponse?)> Withdraw(WithdrawRequest request)
    {
        if (request.Amount <= 0)
        {
            return (ProblemDetailTemplate("Amount cant be <= 0"), null);
        }
        var wallet = await _db.Wallets.Include(q=>q.User).Where(q => q.UserId == request.UserId).FirstOrDefaultAsync();
        if (wallet == null)
        {
            return (ProblemDetailTemplate("Wallet doesn't exist"), null);
        }
        var referenceNo = $"payout-{Guid.NewGuid()}";
        var transaction = new WalletTransaction
        {
            TransactionId = Guid.NewGuid(),
            WalletId = wallet.WalletId,
            AmountMinor = request.Amount,
            Direction = "D",
            SignedAmount = -request.Amount,
            Type = "Withdrawal",
            Status = TransactionStatuses.PENDING,
            CreatedAt = DateTimeOffset.Now,
            IdempotencyKey = Guid.NewGuid().ToString(),
            ExternalRef = referenceNo,
            ReferenceType = "IrisPayout",
            Memo = "Payout via Midtrans Iris"
        };
        _db.WalletTransactions.Add(transaction);
        await _db.SaveChangesAsync();
        //var iris = await _midtransService.CreateWithdrawalAsync(referenceNo, request.Amount, request.BankCode, request.Account, request.Name, wallet.User.Email);
        //if (iris == null)
        //{
        //    return (ProblemDetailTemplate("Something wrong with creating the iris payout"), null);
        //}
        return (null, new TransactionResponse
        {
            orderId = referenceNo,
            paymentStatus = TransactionStatuses.PENDING,
        });
    }
    
    public async Task<long> GetWalletBalance(GetWalletRequest request)
    {
        var wallet = await _db.Wallets.Where(q => q.UserId == request.UserId).FirstOrDefaultAsync();
        return wallet.BalanceMinor;
    }

    public async Task<(ProblemDetails?, TransactionResponse?)> Deposit(DepositRequest request)
    {
        if (request.Amount <= 0)
        {
            return (ProblemDetailTemplate("Amount cant be <= 0"), null);
        }

        var wallet = await _db.Wallets.Include(q => q.User).Where(q => q.UserId == request.UserId)
            .FirstOrDefaultAsync();
        if (wallet == null)
        {
            return (ProblemDetailTemplate("Wallet doesn't exist"), null);
        }

        var existingTransaction = await _db.WalletTransactions.FirstOrDefaultAsync(q =>
            q.WalletId == wallet.WalletId &&
            q.IdempotencyKey == request.IdempotencyKey.ToString());
        if (existingTransaction != null)
        {
            if (existingTransaction.Status == TransactionStatuses.PENDING)
            {
                _db.WalletTransactions.Remove(existingTransaction);
                var newOrderId = $"deposit-{Guid.NewGuid()}";
                var newTransaction = new WalletTransaction
                {
                    TransactionId = Guid.NewGuid(),
                    WalletId = wallet.WalletId,
                    AmountMinor = request.Amount,
                    Direction = "C",
                    SignedAmount = request.Amount,
                    Type = "Deposit",
                    Status = TransactionStatuses.PENDING,
                    CreatedAt = DateTimeOffset.Now,
                    IdempotencyKey = request.IdempotencyKey.ToString(),
                    ExternalRef = newOrderId,
                    ReferenceType = "MidtransSnap",
                    Memo = "Deposit via Midtrans Snap"
                };
                _db.WalletTransactions.Add(newTransaction);
                await _db.SaveChangesAsync();
                var newSnap = await _midtransService.CreateSnapTransactionAsync(newOrderId, request.Amount,
                    wallet.User.Email, wallet.User.UserName);
                if (newSnap == null)
                {
                    return (ProblemDetailTemplate("Something wrong with creating the midtrans transaction"), null);
                }

                return (null, new TransactionResponse
                {
                    orderId = newOrderId,
                    token = newSnap.token,
                    redirectUrl = newSnap.redirect_url,
                    paymentStatus = TransactionStatuses.PENDING,
                });
            }

            return (null, new TransactionResponse
            {
                orderId = existingTransaction.ExternalRef,
                paymentStatus = existingTransaction.Status,
            });
        }

        var orderId = $"deposit-{Guid.NewGuid()}";
        var transaction = new WalletTransaction
        {
            TransactionId = Guid.NewGuid(),
            WalletId = wallet.WalletId,
            AmountMinor = request.Amount,
            Direction = "C",
            SignedAmount = request.Amount,
            Type = "Deposit",
            Status = TransactionStatuses.PENDING,
            CreatedAt = DateTimeOffset.Now,
            IdempotencyKey = request.IdempotencyKey.ToString(),
            ExternalRef = orderId,
            ReferenceType = "MidtransSnap",
            Memo = "Deposit via Midtrans Snap"
        };
        _db.WalletTransactions.Add(transaction);
        await _db.SaveChangesAsync();
        var snap = await _midtransService.CreateSnapTransactionAsync(orderId, request.Amount, wallet.User.Email,
            wallet.User.UserName);
        if (snap == null)
        {
            return (ProblemDetailTemplate("Something wrong with creating the midtrans transaction"), null);
        }

        return (null, new TransactionResponse
        {
            orderId = orderId,
            token = snap.token,
            redirectUrl = snap.redirect_url,
            paymentStatus = TransactionStatuses.PENDING,
        });
    }

    public async Task<(ProblemDetails?, TransactionResponse?)> ApproveAndPayStep(ApproveAndPayStepRequest request)
    {
        var step = await _db.Steps.Include(q => q.Process)
            .Include(q => q.Transaction)
            .Where(q => q.StepId == request.StepId).FirstOrDefaultAsync();
        if (step == null)
        {
            return (ProblemDetailTemplate("Step doesn't exist"), null);
        }

        var buyerId = await _db.Contents.Where(q => q.ContentId == step.Process.RequestId).Select(q => q.AuthorId)
            .FirstOrDefaultAsync();
        var buyerWallet = await _db.Wallets.Include(q => q.User).Where(q => q.UserId == buyerId).FirstOrDefaultAsync();
        if (buyerWallet == null)
        {
            return (ProblemDetailTemplate("Buyer wallet doesn't exist"), null);
        }

        var sellerId = await _db.Requests.Include(q => q.Seller).Where(q => q.RequestId == step.Process.RequestId)
            .Select(q => q.Seller.OwnerId).FirstOrDefaultAsync();
        var sellerWallet = await _db.Wallets.Where(q => q.UserId == sellerId).FirstOrDefaultAsync();
        if (sellerWallet == null)
        {
            return (ProblemDetailTemplate("Seller wallet doesn't exist"), null);
        }

        if (request.Method == "Wallet")
        {
            if (buyerWallet.BalanceMinor < step.Amount)
            {
                return (ProblemDetailTemplate("Insufficient funds"), null);
            }

            var walletTransaction = new WalletTransaction
            {
                TransactionId = Guid.NewGuid(),
                WalletId = buyerWallet.WalletId,
                AmountMinor = step.Amount,
                CreatedAt = DateTimeOffset.Now,
                IdempotencyKey = Guid.NewGuid().ToString(),
                Type = "Fee",
                Status = TransactionStatuses.POSTED,
                PostedAt = DateTime.Now,
                Direction = "D",
                SignedAmount = -step.Amount,
                ReferenceType = "Wallet",
                Memo = "Step payment via wallet"
            };
            buyerWallet.BalanceMinor -= step.Amount;
            step.Status = StepStatuses.WORKING;
            step.UpdatedAt = DateTimeOffset.Now;
            step.TransactionId = walletTransaction.TransactionId;
            _db.WalletTransactions.Add(walletTransaction);
            await _db.SaveChangesAsync();
            await _notificationService.SendNotification("Step has been accepted", sellerId);
            return (null, new TransactionResponse
            {
                orderId = walletTransaction.TransactionId.ToString(),
                paymentStatus = TransactionStatuses.POSTED
            });
        }

        if (step.Transaction != null)
        {
            if (step.Transaction.Status == TransactionStatuses.POSTED)
            {
                return (null, new TransactionResponse
                {
                    orderId = step.Transaction.ExternalRef,
                    paymentStatus = TransactionStatuses.POSTED
                });
            }
        }

        var orderId = $"fee-{Guid.NewGuid()}";
        var transaction = new WalletTransaction
        {
            TransactionId = Guid.NewGuid(),
            WalletId = sellerWallet.WalletId,
            AmountMinor = step.Amount,
            Direction = "C",
            SignedAmount = step.Amount,
            Type = "Fee",
            Status = TransactionStatuses.PENDING,
            CreatedAt = DateTimeOffset.Now,
            IdempotencyKey = Guid.NewGuid().ToString(),
            ExternalRef = orderId,
            ReferenceType = "MidtransSnap",
            Memo = "Payment via Midtrans Snap"
        };
        step.UpdatedAt = DateTimeOffset.Now;
        step.TransactionId = transaction.TransactionId;
        _db.WalletTransactions.Add(transaction);
        await _db.SaveChangesAsync();
        await _notificationService.SendNotification("Step has been accepted", sellerId);
        var snap = await _midtransService.CreateSnapTransactionAsync(orderId, step.Amount,
            email: buyerWallet.User.Email, firstName: buyerWallet.User.UserName);
        if (snap == null)
        {
            return (ProblemDetailTemplate("Something went wrong when creating midtrans transaction"), null);
        }

        return (null, new TransactionResponse
        {
            orderId = orderId,
            token = snap.token,
            redirectUrl = snap.redirect_url,
            paymentStatus = TransactionStatuses.PENDING
        });
    }
}