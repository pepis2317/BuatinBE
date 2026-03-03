using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Enum;
using ThesisTestAPI.Models.Biteship;
using ThesisTestAPI.Models.Process;
using ThesisTestAPI.Models.Shipment;

namespace ThesisTestAPI.Services;

public class ShipmentService
{
    private readonly ThesisDbContext _db;
    private readonly BiteshipOptions _opt;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly HttpClient _httpClient;
    private readonly NotificationService _notificationService;
    private readonly BlobStorageService _blobStorageService;
    private readonly MidtransService _midtransService;
    private readonly BiteshipService _biteshipService;

    public ShipmentService(ThesisDbContext db, BiteshipOptions opt, IHttpContextAccessor httpContextAccessor,
        HttpClient httpClient, NotificationService notificationService, BlobStorageService blobStorageService,
        MidtransService midtransService, BiteshipService biteshipService)
    {
        _db = db;
        _opt = opt;
        _httpContextAccessor = httpContextAccessor;
        _httpClient = httpClient;
        _notificationService = notificationService;
        _blobStorageService = blobStorageService;
        _midtransService = midtransService;
        _biteshipService = biteshipService;
    }

    private ProblemDetails ProblemDetailTemplate(string detail)
    {
        return new ProblemDetails
        {
            Type = "http://veryCoolAPI.com/errors/invalid-data",
            Title = "Shipment error",
            Detail = detail,
            Instance = _httpContextAccessor.HttpContext?.Request.Path
        };
    }
    
    public async Task<(ProblemDetails?, ShipmentResponse?)> SendShipment(SendShipmentRequest request)
    {
        var shipment = await _db.Shipments.Where(q => q.ShipmentId == request.ShipmentId).FirstOrDefaultAsync();
        var orderResponse = await _biteshipService.CreateOrder(shipment.ShipmentId, shipment.OriginNote, shipment.DestinationNote, "now", shipment.CourierCompany, shipment.CourierType, shipment.OrderNote);
        if (orderResponse == null)
        {
            return (ProblemDetailTemplate("Problem in creating biteship order"), null);
        }
        shipment.OrderId = orderResponse.Id;
        shipment.Status = ShipmentStatuses.SENT;
        await _db.SaveChangesAsync();
        return (null, new ShipmentResponse
        {
            ShipmentId = shipment.ShipmentId
        });
    }

    public async Task<(ProblemDetails?, ShipmentResponse?)> PayShipment(PayShipmentRequest request)
    {
        var shipment = await _db.Shipments.Include(q => q.Transaction).Include(q => q.Process)
            .ThenInclude(q => q.Request).ThenInclude(q => q.Seller).Where(q => q.ShipmentId == request.ShipmentId)
            .FirstOrDefaultAsync();
        if (shipment == null)
        {
            return (ProblemDetailTemplate("Shipping data doesn't exist"), null);
        }

        var buyerId = await _db.Contents.Where(q => q.ContentId == shipment.Process.Request.RequestId)
            .Select(q => q.AuthorId).FirstOrDefaultAsync();
        var buyerWallet = await _db.Wallets.Include(q => q.User).Where(q => q.UserId == buyerId).FirstOrDefaultAsync();
        if (buyerWallet == null)
        {
            return (ProblemDetailTemplate("Buyer wallet doesn't exist"), null);
        }

        if (request.Method == "Wallet")
        {
            var walletTransaction = new WalletTransaction
            {
                TransactionId = Guid.NewGuid(),
                WalletId = buyerWallet.WalletId,
                AmountMinor = request.Amount,
                CreatedAt = DateTimeOffset.Now,
                IdempotencyKey = Guid.NewGuid().ToString(),
                Type = "Fee",
                Status = TransactionStatuses.POSTED,
                PostedAt = DateTime.Now,
                Direction = "D",
                SignedAmount = -request.Amount,
                ReferenceType = "Wallet",
                Memo = "Shipment payment via wallet"
            };
            buyerWallet.BalanceMinor -= request.Amount;
            shipment.TransactionId = walletTransaction.TransactionId;
            shipment.Status = ShipmentStatuses.PAID;
            shipment.UpdatedAt = DateTime.Now;
            shipment.OriginNote = request.OriginNote;
            shipment.DestinationNote = request.DestinationNote;
            shipment.OrderNote = request.OrderNote;
            shipment.DestinationNote = request.DestinationNote;
            shipment.CourierType = request.CourierType;
            shipment.CourierCompany = request.CourierCompany;
            _db.WalletTransactions.Add(walletTransaction);
            await _db.SaveChangesAsync();
            return (null, new ShipmentResponse
            {
                ShipmentId = shipment.ShipmentId,
                paymentStatus = TransactionStatuses.POSTED
            });
        }

        if (shipment.Transaction != null)
        {
            if (shipment.Transaction.Status == TransactionStatuses.POSTED)
            {
                return (null, new ShipmentResponse
                {
                    ShipmentId = shipment.ShipmentId,
                    paymentStatus = TransactionStatuses.POSTED
                });
            }
        }

        var orderId = $"fee-{Guid.NewGuid()}";
        var transaction = new WalletTransaction
        {
            TransactionId = Guid.NewGuid(),
            Status = TransactionStatuses.PENDING,
            CreatedAt = DateTimeOffset.Now,
            IdempotencyKey = Guid.NewGuid().ToString(),
            ExternalRef = orderId,
            ReferenceType = "MidtransSnap",
            Memo = "Shipment payment via Midtrans Snap",
            WalletId = buyerWallet.WalletId,
            AmountMinor = request.Amount,
            Type = "Fee",
            Direction = "D",
            SignedAmount = -request.Amount,
        };
        shipment.TransactionId = transaction.TransactionId;
        shipment.OriginNote = request.OriginNote;
        shipment.DestinationNote = request.DestinationNote;
        shipment.OrderNote = request.OrderNote;
        shipment.CourierType = request.CourierType;
        shipment.DestinationNote = request.DestinationNote;
        shipment.CourierCompany = request.CourierCompany;
        _db.WalletTransactions.Add(transaction);
        await _db.SaveChangesAsync();
        var snap = await _midtransService.CreateSnapTransactionAsync(orderId, request.Amount,
            email: buyerWallet.User.Email, firstName: buyerWallet.User.UserName);
        if (snap == null)
        {
            return (ProblemDetailTemplate("Something went wrong when creating midtrans transaction"), null);
        }

        await _notificationService.SendNotification("Shipping fee has been paid by buyer",
            shipment.Process.Request.Seller.OwnerId);
        return (null, new ShipmentResponse
        {
            ShipmentId = shipment.ShipmentId,
            token = snap.token,
            redirectUrl = snap.redirect_url,
            paymentStatus = TransactionStatuses.PENDING
        });
    }

    public async Task<PaginatedProcessesResponse> GetShippable(GetShippableRequest request)
    {
        var completedProcesses = await _db.Processes
            .Skip((request.pageNumber - 1) * request.pageSize)
            .Include(q => q.Shipments)
            .Include(q => q.Request).ThenInclude(q => q.Seller)
            .Include(q => q.Request).ThenInclude(q => q.RequestNavigation).ThenInclude(q => q.Author)
            .Where(q => q.Request.Seller.OwnerId == request.UserId && q.Status == ProcessStatuses.COMPLETED &&
                        q.Shipments.Count == 0)
            .ToListAsync();
        var list = new List<ProcessResponse>();
        foreach (var process in completedProcesses)
        {
            var item = new ProcessResponse
            {
                ProcessId = process.ProcessId,
                Description = process.Description,
                Status = process.Status,
                Title = process.Title
            };
            var user = process.Request.RequestNavigation.Author;
            var pfp = "";
            if (!string.IsNullOrEmpty(user.Pfp))
            {
                pfp = await _blobStorageService.GetTemporaryImageUrl(user.Pfp, Enum.BlobContainers.PFP);
            }

            item.User = new Models.User.UserResponse
            {
                UserId = user.UserId,
                UserName = user.UserName,
                Pfp = pfp
            };
            list.Add(item);
        }

        var total = await _db.Processes.Where(q =>
                q.Request.Seller.OwnerId == request.UserId && q.Status == ProcessStatuses.COMPLETED &&
                q.Shipments == null)
            .CountAsync();
        return new PaginatedProcessesResponse
        {
            Total = total,
            Processes = list
        };
    }

    public async Task<PaginatedShipmentResponse> GetShipments(GetShipmentsRequest request)
    {
        var processIds = await _db.Processes
            .Where(q => q.Request.RequestNavigation.AuthorId == request.UserId)
            .Select(q => q.ProcessId)
            .ToListAsync();

        var query = _db.Shipments.Where(q => processIds.Contains(q.ProcessId));

        var total = await query.CountAsync();

        var shipments = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((request.pageNumber - 1) * request.pageSize)
            .Take(request.pageSize)
            .Select(shipment => new ShipmentResponse
            {
                ShipmentId = shipment.ShipmentId,
                Name = shipment.Name,
                Description = shipment.Description,
                Status = shipment.Status,
                OrderId = shipment.OrderId
            })
            .ToListAsync();

        return new PaginatedShipmentResponse
        {
            Total = total,
            Shipments = shipments
        };
    }

    public async Task<ShipmentResponse> GetShipment(GetShipmentRequest request)
    {
        var shipment = await _db.Shipments.Where(q => q.ShipmentId == request.ShipmentId).Select(q =>
            new ShipmentResponse
            {
                ShipmentId = q.ShipmentId,
                Name = q.Name,
                Description = q.Description,
                OrderId = q.OrderId,
                Status = q.Status,
                Quantity = q.Quantity,
                Height = q.Height,
                Length = q.Length,
                Weight = q.Weight,
                Width = q.Width,
                Category = q.Category,
                CourierCompany = q.CourierCompany,
                CourierType = q.CourierType,
                OrderNote = q.OrderNote,
                OriginNote = q.OriginNote,
                DestinationNote = q.DestinationNote,
            }).FirstOrDefaultAsync();
        return shipment;
    }

    public async Task<PaginatedShipmentResponse> GetSellerShipments(GetSellerShipmentsRequest request)
    {
        var processIds = await _db.Processes
            .Where(q => q.Request.Seller.OwnerId == request.UserId)
            .Select(q => q.ProcessId)
            .ToListAsync();

        var query = _db.Shipments.Where(q => processIds.Contains(q.ProcessId));

        var total = await query.CountAsync();

        var shipments = await query
            .OrderByDescending(q => q.CreatedAt)
            .Skip((request.pageNumber - 1) * request.pageSize)
            .Take(request.pageSize)
            .Select(shipment => new ShipmentResponse
            {
                ShipmentId = shipment.ShipmentId,
                Name = shipment.Name,
                Description = shipment.Description,
                Status = shipment.Status,
                OrderId = shipment.OrderId
            })
            .ToListAsync();

        return new PaginatedShipmentResponse
        {
            Total = total,
            Shipments = shipments
        };
    }

    public async Task<PaginatedShipmentResponse> GetAllShipments(GetAllShipmentsRequest request)
    {
        var processIds = await _db.Processes.Include(q => q.Request).ThenInclude(q => q.RequestNavigation)
            .Select(q => q.ProcessId).ToListAsync();
        var shipments = await _db.Shipments.Skip((request.pageNumber - 1) * request.pageSize)
            .Where(q => processIds.Contains(q.ProcessId)).OrderByDescending(q => q.CreatedAt).ToListAsync();
        var list = new List<ShipmentResponse>();
        foreach (var shipment in shipments)
        {
            var item = new ShipmentResponse
            {
                ShipmentId = shipment.ShipmentId,
                Name = shipment.Name,
                Description = shipment.Description,
                Status = shipment.Status,
                OrderId = shipment.OrderId
            };
            list.Add(item);
        }

        var total = await _db.Shipments.Where(q => processIds.Contains(q.ProcessId)).CountAsync();

        return new PaginatedShipmentResponse
        {
            Total = total,
            Shipments = list
        };
    }

    public async Task<(ProblemDetails?, ShipmentResponse?)> CreateShipment(CreateShipmentRequest request)
    {
        var process = await _db.Processes.Include(q => q.Request).ThenInclude(q => q.RequestNavigation)
            .Include(q => q.Steps).Where(q => q.ProcessId == request.ProcessId).FirstOrDefaultAsync();
        if (process == null)
        {
            return (ProblemDetailTemplate("Invalid process"), null);
        }

        long amount = 0;
        foreach (var step in process.Steps)
        {
            if (step.Status == StepStatuses.COMPLETED || step.Status == StepStatuses.WORKING)
            {
                amount += step.Amount;
            }
        }

        var shipment = new ThesisTestAPI.Entities.Shipment
        {
            ShipmentId = Guid.NewGuid(),
            ProcessId = request.ProcessId,
            Name = request.Name,
            Description = request.Description,
            Category = request.Category,
            Quantity = request.Quantity,
            Height = request.Height,
            Width = request.Width,
            Weight = request.Weight,
            Length = request.Length,
            Status = ShipmentStatuses.PENDING,
            Value = amount,
            CreatedAt = DateTimeOffset.Now,
        };
        _db.Shipments.Add(shipment);
        await _db.SaveChangesAsync();
        await _notificationService.SendNotification($"Shipment request created for process {process.Title}",
            process.Request.RequestNavigation.AuthorId);
        return (null, new ShipmentResponse
        {
            ShipmentId = shipment.ShipmentId
        });
    }

    public async Task<(ProblemDetails?, OrderCreatedResponse?)> CreateShipmentByCoordinates(
        CreateShipmentByCoordinatesRequest request)
    {
        var sender = await _db.Users.Where(q => q.UserId == request.OriginUserId).FirstOrDefaultAsync();
        if (sender == null)
        {
            return (ProblemDetailTemplate("Invalid sender"), null);
        }

        var receiver = await _db.Users.Where(q => q.UserId == request.DestinationUserId).FirstOrDefaultAsync();
        if (receiver == null)
        {
            return (ProblemDetailTemplate("Invalid receiver"), null);
        }

        if (string.IsNullOrEmpty(sender.Address) || sender.Location == null)
        {
            return (ProblemDetailTemplate("Sender lacks address or postal code"), null);
        }

        if (string.IsNullOrEmpty(receiver.Address) || receiver.Location == null)
        {
            return (ProblemDetailTemplate("Receiver lacks address or postal code"), null);
        }

        var orderRequest = new BiteshipOrderBody
        {
            origin_contact_name = sender.UserName,
            origin_contact_phone = sender.Phone,
            origin_contact_email = sender.Email,
            origin_address = sender.Address,
            origin_note = request.OriginNote,
            origin_coordinate = new Models.Shipment.BiteshipCoordinate
            {
                latitude = sender.Location.Coordinate.Y,
                longitude = sender.Location.Coordinate.X,
            },
            destination_contact_name = receiver.UserName,
            destination_contact_phone = receiver.Phone,
            destination_contact_email = receiver.Email,
            destination_address = receiver.Address,
            destination_note = request.DestinationNote,
            destination_coordinate = new Models.Shipment.BiteshipCoordinate
            {
                latitude = receiver.Location.Coordinate.Y,
                longitude = receiver.Location.Coordinate.X,
            },
            delivery_type = request.DeliveryType,
            order_note = request.OrderNote,
        };
        foreach (var item in request.Items)
        {
            var orderItem = new BiteshipItem
            {
                name = item.Name,
                description = item.Description,
                value = item.Value,
                quantity = item.Quantity,
                height = item.Height,
                length = item.Length,
                weight = item.Weight,
                width = item.Width
            };
            orderRequest.items.Add(orderItem);
        }

        var json = System.Text.Json.JsonSerializer.Serialize(orderRequest);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(_opt.ApiKey);
        var response = await _httpClient.PostAsync("https://api.biteship.com/v1/orders", content);
        var body = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            return (null, new OrderCreatedResponse
            {
                Result = "Failed",
                Response = System.Text.Json.JsonSerializer.Serialize(body)
            });
        }

        await _notificationService.SendNotification("Product has been shipped by seller", receiver.UserId);
        return (null, new OrderCreatedResponse
        {
            Result = "Success",
            Response = System.Text.Json.JsonSerializer.Serialize(body)
        });
    }
}