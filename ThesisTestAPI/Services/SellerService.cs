using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Microsoft.AspNetCore.Mvc;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Producer;
using ThesisTestAPI.Models.Seller;

namespace ThesisTestAPI.Services
{
    public class SellerService
    {
        private readonly ThesisDbContext _db;
        private readonly BlobStorageService _blobStorageService;
        private readonly IDataProtector _protector;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserService _userService;
        public SellerService(ThesisDbContext db, BlobStorageService blobStorageService, IDataProtectionProvider provider, IHttpContextAccessor httpContextAccessor, UserService userService)
        {
            _db = db;
            _blobStorageService = blobStorageService;
            _httpContextAccessor = httpContextAccessor;
            _userService = userService;
            _protector = provider.CreateProtector("CredentialsProtector");
        }
        
        public async Task<PaginatedSellersResponse> GetSellersByQuery(SellerQuery request)
        {
            Point? userLocation = null;

            if (request.latitude.HasValue && request.longitude.HasValue)
            {
                userLocation = new Point(request.longitude.Value, request.latitude.Value) { SRID = 4326 };
            }

            var query = _db.Sellers.Include(q=>q.Owner).AsQueryable();

            if (!string.IsNullOrEmpty(request.searchTerm))
            {
                request.searchTerm = request.searchTerm.ToLower();
                query = query.Where(q => q.SellerName.ToLower().Contains(request.searchTerm));
            }

            if (userLocation != null)
            {
                query = query.OrderBy(p => p.Owner.Location.Distance(userLocation));
            }

            var totalSellers = await query.CountAsync();
            var Sellers = await query
                .Skip((request.pageNumber - 1) * request.pageSize)
                .Take(request.pageSize)
                .ToListAsync();
            var sellerIds = Sellers.Select(q=>q.SellerId).ToList();
            var avgPerSeller = await
                (from sr in _db.SellerReviews
                 where sellerIds.Contains(sr.SellerId)
                 join r in _db.Ratings.Include(x => x.RatingNavigation)
                     on sr.SellerReviewId equals r.RatingNavigation.ContentId
                 group r by sr.SellerId into g
                 select new
                 {
                     SellerId = g.Key,
                     AvgRating = g.Average(x => (double)x.Rating1), // <— change x.Value to your rating field
                     RatingCount = g.Count()
                 })
                .ToListAsync();

            // 2) Map back to your paged sellers (fill 0 if no ratings yet)
            var avgDict = avgPerSeller.ToDictionary(x => x.SellerId);
            var sellersWithAvg = Sellers.Select(s => new
            {
                s.SellerId,
                AvgRating = avgDict.TryGetValue(s.SellerId, out var v) ? v.AvgRating : 0.0,
                RatingCount = avgDict.TryGetValue(s.SellerId, out var v2) ? v2.RatingCount : 0
            }).ToList();
            var perSellerClientCounts = await _db.Requests
                .Where(r => sellerIds.Contains(r.SellerId)
                            && r.RequestStatus == Enum.RequestStatuses.ACCEPTED)
                .GroupBy(r => r.SellerId)
                .Select(g => new {
                    SellerId = g.Key,
                    ClientCount = g.Select(r => r.RequestId).Distinct().Count()
                })
                .ToListAsync();

            var SellersList = new List<SellerResponse>();
            foreach (var Seller in Sellers)
            {
                var rating = sellersWithAvg.Where(q=>q.SellerId == Seller.SellerId).Select(q=>q.AvgRating).FirstOrDefault();
                var clients = perSellerClientCounts.Where(q => q.SellerId == Seller.SellerId).Select(q => q.ClientCount).FirstOrDefault();
                var SellerResponse = new SellerResponse
                {
                    SellerId = Seller.SellerId,
                    SellerName = Seller.SellerName,
                    Rating = rating,
                    Clients = clients
                };
                var owner = await _userService.Get(Seller.OwnerId);
                if(owner != null)
                {
                    SellerResponse.Owner = owner;
                }
                var picture = await PictureHelper(Seller.SellerPicture, Enum.BlobContainers.SELLERPICTURE);
                if (picture != null)
                {
                    SellerResponse.SellerPicture = picture;
                }
                var banner = await PictureHelper(Seller.SellerPicture, Enum.BlobContainers.BANNER);
                if(banner != null)
                {
                    SellerResponse.Banner = banner;
                }
                SellersList.Add(SellerResponse);
            }
            return new PaginatedSellersResponse
            {
                Total = totalSellers,
                Sellers = SellersList
            };
        }
        
        public async Task<string> HandleSellerPicture(UploadSellerPictureRequest request)
        {
            var fileName = $"{Guid.NewGuid()}_{request.File.FileName}";
            var contentType = request.File.ContentType;
            using var stream = request.File.OpenReadStream();

            var Seller = await _db.Sellers.FirstOrDefaultAsync(q => q.SellerId == request.SellerId);
            if (Seller != null)
            {
                if (!string.IsNullOrEmpty(Seller.SellerPicture))
                {
                    await _blobStorageService.DeleteFileAsync(Seller.SellerPicture, Enum.BlobContainers.SELLERPICTURE);
                }
                string url = await _blobStorageService.UploadImageAsync(stream, fileName, contentType, Enum.BlobContainers.SELLERPICTURE, 200);
                Seller.SellerPicture = url;
                _db.Sellers.Update(Seller);
                await _db.SaveChangesAsync();
                return url;
            }
            return "failed to upload Seller image";
        }
        public async Task<SellerResponse> GetSellerFromOwnerId(GetSellerFromOwnerIdRequest request)
        {
            var Seller = await _db.Sellers.Where(q => q.OwnerId == request.OwnerId).FirstOrDefaultAsync();
            var sellerPicture = "";
            if(!string.IsNullOrEmpty(Seller.SellerPicture))
            {
                sellerPicture = await _blobStorageService.GetTemporaryImageUrl(Seller.SellerPicture, Enum.BlobContainers.SELLERPICTURE);
            }
            var banner = "";
            if (!string.IsNullOrEmpty(Seller.Banner))
            {
                banner = await _blobStorageService.GetTemporaryImageUrl(Seller.Banner, Enum.BlobContainers.BANNER);
            }
            var owner = await _userService.Get(Seller.OwnerId);
            return new SellerResponse
            {
                SellerId = Seller.SellerId,
                SellerName = Seller.SellerName,
                SellerPicture = sellerPicture,
                Owner = owner,
                Banner = banner,
                Description = Seller.Description,
                CreatedAt = (DateTime) Seller.CreatedAt
            };
        }
        private async Task<string?> PictureHelper(string? fileName, string container)
        {
            string? imageUrl = await _blobStorageService.GetTemporaryImageUrl(fileName, container);
            return imageUrl;
        }
        
        public async Task<(ProblemDetails?, SellerResponse?)> GetSellerById(GetSellerRequest request)
        {
            var Seller = await _db.Sellers.Where(q => q.SellerId == request.SellerId).FirstOrDefaultAsync();
            if(Seller == null)
            {
                var problemDetails = new ProblemDetails
                {
                    Type = "http://veryCoolAPI.com/errors/invalid-data",
                    Title = "Invalid Request Data",
                    Detail = string.Join("; ", "no Seller with such id exists"),
                    Instance = _httpContextAccessor.HttpContext?.Request.Path
                };
                return (problemDetails, null);
            }
            var owner = await _userService.Get(Seller.OwnerId);
            var picture = await PictureHelper(Seller.SellerPicture, Enum.BlobContainers.SELLERPICTURE);
            var banner = await PictureHelper(Seller.Banner, Enum.BlobContainers.BANNER);
            return (null, new SellerResponse()
            {
                SellerId = Seller.SellerId,
                SellerName = Seller.SellerName,
                Owner = owner,
                SellerPicture = picture,
                Banner = banner,
                Description = Seller.Description,
                CreatedAt = (DateTime) Seller.CreatedAt
            });
            
        }
        public async Task<string>HandleBanner(BannerRequest request)
        {
            var fileName = $"{Guid.NewGuid()}_{request.File.FileName}";
            var contentType = request.File.ContentType;
            using var stream = request.File.OpenReadStream();

            var producer = await _db.Sellers.FirstOrDefaultAsync(q => q.SellerId == request.SellerId);
            if (producer != null)
            {
                if (!string.IsNullOrEmpty(producer.Banner))
                {
                    await _blobStorageService.DeleteFileAsync(producer.SellerPicture, Enum.BlobContainers.BANNER);
                }
                string url = await _blobStorageService.UploadImageAsync(stream, fileName, contentType, Enum.BlobContainers.BANNER, 200);
                producer.Banner = url;
                _db.Sellers.Update(producer);
                await _db.SaveChangesAsync();
                return url;
            }
            return "failed to upload seller banner";
        }
        
        public async Task<List<SellerResponse>> GetAllSellers()
        {
            var Sellers = await _db.Sellers.Select(x => new SellerResponse() {
                SellerId = x.SellerId,
                SellerName = x.SellerName,
            }).ToListAsync();
            return Sellers;
        }

        public async Task<SellerResponse?> CreateSeller(CreateSellerRequest request)
        {
            if (!request.Latitude.HasValue || !request.Longitude.HasValue)
            {
                return null;
            }
            var userId = Guid.NewGuid();
            var user = new User
            {
                UserId = userId,
                UserName = request.UserName,
                Email = request.Email,
                Phone = request.Phone,
                Password = _protector.Protect(request.Password),
                Role = request.Role,
                Address = request.Address,
                PostalCode = request.PostalCode,
                Location = new Point(request.Longitude.Value, request.Latitude.Value) { SRID = 4326 },
            };
            var wallet = new Wallet
            {
                WalletId = Guid.NewGuid(),
                UserId = user.UserId,
                Currency = "IDR",
                BalanceMinor = 0,
                Status = "Active",
                CreatedAt = DateTime.UtcNow,
            };

            _db.Users.Add(user);
            _db.Wallets.Add(wallet);
            var SellerId = Guid.NewGuid();

            var Seller = new Seller
            {
                OwnerId = userId,
                SellerName = request.SellerName,
                SellerId = SellerId,
                Banner = null,
                CreatedAt = DateTime.Now
            };

            _db.Sellers.Add(Seller);
            await _db.SaveChangesAsync();

            return new SellerResponse
            {
                SellerId = Seller.SellerId,
                SellerName = Seller.SellerName,
            };
        }
        public async Task<SellerResponse?> EditSeller(EditSellerRequest request)
        {
            var Seller = await _db.Sellers.FirstOrDefaultAsync(q => q.SellerId == request.SellerId);
            if(Seller == null)
            {
                return null;
            }
            Seller.SellerName = string.IsNullOrEmpty(request.SellerName) ? Seller.SellerName : request.SellerName;
            Seller.Description = string.IsNullOrEmpty(request.Description) ? Seller.Description : request.Description;
            if(request.Latitude.HasValue && request.Longitude.HasValue)
            {
                Seller.Location = new Point(request.Longitude.Value, request.Latitude.Value) { SRID = 4326 };
            }
            if (request.SellerPicture != null)
            {
                var sellerPictureName = $"{Guid.NewGuid()}_{request.SellerPicture.FileName}";
                var contentType = request.SellerPicture.ContentType;
                using var sellerPictureStream = request.SellerPicture.OpenReadStream();
                if (!string.IsNullOrEmpty(Seller.SellerPicture))
                {
                    await _blobStorageService.DeleteFileAsync(Seller.SellerPicture, Enum.BlobContainers.SELLERPICTURE);
                }
                await _blobStorageService.UploadImageAsync(sellerPictureStream, sellerPictureName, contentType, Enum.BlobContainers.SELLERPICTURE, 200);
                Seller.SellerPicture = sellerPictureName;
            }
            if(request.Banner != null)
            {
                var bannerName = $"{Guid.NewGuid()}_{request.Banner.FileName}";
                var contentType = request.Banner.ContentType;
                using var bannerStream = request.Banner.OpenReadStream();
                if (!string.IsNullOrEmpty(Seller.Banner)){
                    await _blobStorageService.DeleteFileAsync(Seller.Banner, Enum.BlobContainers.BANNER);
                }
                await _blobStorageService.UploadImageFreeAspectAsync(bannerStream, bannerName, contentType, Enum.BlobContainers.BANNER);
                Seller.Banner = bannerName;
            }
            _db.Sellers.Update(Seller);
            await _db.SaveChangesAsync();
            return new SellerResponse
            {
                SellerId = Seller.SellerId,
                SellerName = Seller.SellerName,
            };
        }
    }
}
