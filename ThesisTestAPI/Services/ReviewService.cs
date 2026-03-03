using Microsoft.EntityFrameworkCore;
using ThesisTestAPI.Entities;
using ThesisTestAPI.Models.Review;

namespace ThesisTestAPI.Services;

public class ReviewService
{
    private readonly ThesisDbContext _db;
    private readonly RatingService _ratingService;
    private readonly BlobStorageService _blobStorageService;

    public ReviewService(ThesisDbContext db, RatingService ratingService, BlobStorageService blobStorageService)
    {
        _db = db;
        _ratingService = ratingService;
        _blobStorageService = blobStorageService;
    }
    
    public async Task<UserStatsResponse> GetUserStats(GetUserStatsRequest request)
    {
        var reviewIds = await _db.UserReviews.Where(q => q.UserId == request.UserId).Select(q => q.UserReviewId).ToListAsync();
        var ratings = await _db.Ratings.Include(q => q.RatingNavigation).Where(q => reviewIds.Contains(q.RatingNavigation.ContentId)).Select(q => q.Rating1).AverageAsync();
        return new UserStatsResponse
        {
            Rating = ratings != null ? (double)ratings : 0,
            Reviews = reviewIds.Count,
        };
    }
    
    public async Task<SellerStatsResponse> GetSellerStats(GetSellerStatsRequest request)
    {
        var reviewIds = await _db.SellerReviews.Where(q => q.SellerId == request.SellerId).Select(q => q.SellerReviewId).ToListAsync();
        var ratings = await _db.Ratings.Include(q => q.RatingNavigation).Where(q=>reviewIds.Contains(q.RatingNavigation.ContentId)).Select(q=>q.Rating1).AverageAsync();
        var uniqueClients = await _db.Requests.Where(r => r.SellerId == request.SellerId && r.RequestStatus == Enum.RequestStatuses.ACCEPTED).Select(r => r.RequestId).Distinct().CountAsync();
        var processes = await _db.Processes.Include(q => q.Request).Where(q => q.Request.SellerId == request.SellerId).ToListAsync();
        var completed = processes.Where(q => q.Status == Enum.ProcessStatuses.COMPLETED);
        double completionRate = 0;
        if (processes.Count() > 0)
        {
            completionRate = (double)completed.Count() / processes.Count();
        }
        return new SellerStatsResponse
        {
            Rating = ratings != null? (double)ratings : 0,
            Clients = uniqueClients,
            Reviews = reviewIds.Count,
            CompletionRate = completionRate
        };
    }

    public async Task<PaginatedReviewsResponse> GetUserReviews(GetUserReviewsRequest request)
    {
        var reviews = await _db.UserReviews
            .Include(q => q.UserReviewNavigation).ThenInclude(q => q.Author)
            .Skip((request.pageNumber - 1) * request.pageSize)
            .Where(q => q.UserId == request.UserId).OrderByDescending(q => q.UserReviewNavigation.CreatedAt)
            .ToListAsync();
        var reviewContentIds = reviews.Select(q => q.UserReviewNavigation.ContentId).ToList();
        var commentCounts = await _db.Comments
            .Where(q => reviewContentIds.Contains(q.TargetContentId))
            .GroupBy(q => q.TargetContentId)
            .Select(g => new
            {
                TargetContentId = g.Key,
                CommentCount = g.Count()
            })
            .ToListAsync();
        var likes = await _db.Likes.Include(q => q.LikeNavigation)
            .Where(q => reviewContentIds.Contains(q.LikeNavigation.ContentId))
            .GroupBy(q => q.LikeNavigation.ContentId).Select(q => new
            {
                ContentId = q.Key,
                Likes = q.Count()
            }).ToListAsync();
        var liked = await _db.Likes.Include(q => q.LikeNavigation).Where(q =>
                reviewContentIds.Contains(q.LikeNavigation.ContentId) && q.LikeNavigation.AuthorId == request.UserId)
            .ToListAsync();
        var ratings = await _db.Ratings.Include(q => q.RatingNavigation)
            .Where(q => reviewContentIds.Contains(q.RatingNavigation.ContentId)).ToListAsync();
        var list = new List<ReviewResponse>();
        foreach (var review in reviews)
        {
            var isLiked = liked.Any(q => q.LikeNavigation.ContentId == review.UserReviewId);
            var likeCount = likes.Where(q => q.ContentId == review.UserReviewId).Select(q => q.Likes).FirstOrDefault();
            var commentCount = commentCounts
                .FirstOrDefault(q => q.TargetContentId == review.UserReviewNavigation.ContentId);
            var rating = ratings
                .FirstOrDefault(q => q.RatingNavigation.ContentId == review.UserReviewNavigation.ContentId);
            var user = review.UserReviewNavigation.Author;
            var pfp = "";
            if (!string.IsNullOrEmpty(user.Pfp))
            {
                pfp = await _blobStorageService.GetTemporaryImageUrl(user.Pfp, Enum.BlobContainers.PFP);
            }

            list.Add(new ReviewResponse
            {
                ReviewId = review.UserReviewId,
                AuthorId = user.UserId,
                AuthorName = user.UserName,
                AuthorPfp = pfp,
                Review = review.Review,
                CreatedAt = review.UserReviewNavigation.CreatedAt,
                UpdatedAt = review.UserReviewNavigation.UpdatedAt,
                Comments = commentCount != null ? commentCount.CommentCount : 0,
                Rating = rating != null ? rating.Rating1 : 0,
                Likes = likeCount,
                Liked = isLiked
            });
        }

        var total = await _db.UserReviews.Where(q => q.UserId == request.UserId).CountAsync();
        return new PaginatedReviewsResponse
        {
            Total = total,
            Reviews = list
        };
    }

    public async Task<PaginatedReviewsResponse> GetSellerReviews(GetSellerReviewsRequest request)
    {
        var reviews = await _db.SellerReviews
            .Include(q => q.SellerReviewNavigation).ThenInclude(q => q.Author)
            .Skip((request.pageNumber - 1) * request.pageSize)
            .Where(q => q.SellerId == request.SellerId).OrderByDescending(q => q.SellerReviewNavigation.CreatedAt)
            .ToListAsync();
        var reviewContentIds = reviews.Select(q => q.SellerReviewNavigation.ContentId).ToList();
        var ratings = await _db.Ratings.Include(q => q.RatingNavigation)
            .Where(q => reviewContentIds.Contains(q.RatingNavigation.ContentId)).ToListAsync();
        var commentCounts = await _db.Comments
            .Where(q => reviewContentIds.Contains(q.TargetContentId))
            .GroupBy(q => q.TargetContentId)
            .Select(g => new
            {
                TargetContentId = g.Key,
                CommentCount = g.Count()
            })
            .ToListAsync();
        var likes = await _db.Likes.Include(q => q.LikeNavigation)
            .Where(q => reviewContentIds.Contains(q.LikeNavigation.ContentId))
            .GroupBy(q => q.LikeNavigation.ContentId).Select(q => new
            {
                ContentId = q.Key,
                Likes = q.Count()
            }).ToListAsync();
        var liked = await _db.Likes.Include(q => q.LikeNavigation).Where(q =>
                reviewContentIds.Contains(q.LikeNavigation.ContentId) && q.LikeNavigation.AuthorId == request.UserId)
            .ToListAsync();
        var list = new List<ReviewResponse>();
        foreach (var review in reviews)
        {
            var isLiked = liked.Any(q => q.LikeNavigation.ContentId == review.SellerReviewId);
            var likeCount = likes.Where(q => q.ContentId == review.SellerReviewId).Select(q => q.Likes)
                .FirstOrDefault();
            var commentCount = commentCounts
                .FirstOrDefault(q => q.TargetContentId == review.SellerReviewNavigation.ContentId);
            var rating = ratings
                .FirstOrDefault(q => q.RatingNavigation.ContentId == review.SellerReviewNavigation.ContentId);
            var user = review.SellerReviewNavigation.Author;
            var pfp = "";
            if (!string.IsNullOrEmpty(user.Pfp))
            {
                pfp = await _blobStorageService.GetTemporaryImageUrl(user.Pfp, Enum.BlobContainers.PFP);
            }

            list.Add(new ReviewResponse
            {
                ReviewId = review.SellerReviewId,
                AuthorId = user.UserId,
                AuthorName = user.UserName,
                AuthorPfp = pfp,
                Review = review.Review,
                CreatedAt = review.SellerReviewNavigation.CreatedAt,
                UpdatedAt = review.SellerReviewNavigation.UpdatedAt,
                Rating = rating != null ? rating.Rating1 : 0,
                Comments = commentCount != null ? commentCount.CommentCount : 0,
                Likes = likeCount,
                Liked = isLiked
            });
        }

        var total = await _db.SellerReviews.Where(q => q.SellerId == request.SellerId).CountAsync();
        return new PaginatedReviewsResponse
        {
            Total = total,
            Reviews = list
        };
    }

    public async Task<string> EditUserReview(EditUserReviewRequest request)
    {
        var review = await _db.UserReviews.Include(q => q.UserReviewNavigation)
            .Where(q => q.UserReviewId == request.ReviewId).FirstOrDefaultAsync();
        var rating = await _db.Ratings.Include(q => q.RatingNavigation)
            .Where(q => q.RatingNavigation.ContentId == review.UserReviewId).FirstOrDefaultAsync();
        review.Review = request.Review;
        rating.Rating1 = request.Rating;
        review.UserReviewNavigation.UpdatedAt = DateTimeOffset.Now;
        _db.UserReviews.Update(review);
        await _db.SaveChangesAsync();
        return $@"Successfully Updated {review.UserReviewId}";
    }

    public async Task<string> EditSellerReview(EditSellerReviewRequest request)
    {
        var review = await _db.SellerReviews.Include(q => q.SellerReviewNavigation)
            .Where(q => q.SellerReviewId == request.ReviewId).FirstOrDefaultAsync();
        var rating = await _db.Ratings.Include(q => q.RatingNavigation)
            .Where(q => q.RatingNavigation.ContentId == review.SellerReviewId).FirstOrDefaultAsync();
        review.Review = request.Review;
        rating.Rating1 = request.Rating;
        review.SellerReviewNavigation.UpdatedAt = DateTimeOffset.Now;
        _db.SellerReviews.Update(review);
        await _db.SaveChangesAsync();
        return $@"Successfully Updated {review.SellerReviewId}";
    }

    public async Task<string> DeleteReview(Guid reviewId)
    {
        await _db.Contents.Where(q => q.ContentId == reviewId).ExecuteDeleteAsync();
        return "Successfully deleted review";
    }

    public async Task<string> CreateReview(Guid authorId, string reviewText, Guid? sellerId, Guid? userId, int rating)
    {
        var contentId = Guid.NewGuid();
        var content = new Content
        {
            ContentId = contentId,
            AuthorId = authorId,
            CreatedAt = DateTimeOffset.UtcNow,
        };
        if (sellerId != null)
        {
            var review = new SellerReview
            {
                SellerReviewId = contentId,
                SellerReviewNavigation = content,
                Review = reviewText,
                SellerId = sellerId.Value
            };
            _db.SellerReviews.Add(review);
        }

        if (userId != null)
        {
            var review = new UserReview
            {
                UserReviewId = contentId,
                UserReviewNavigation = content,
                Review = reviewText,
                UserId = userId.Value
            };
            _db.UserReviews.Add(review);
        }

        await _db.SaveChangesAsync();
        await _ratingService.CreateRating(new Models.Rating.CreateRatingRequest
        {
            AuthorId = authorId,
            Rating = rating,
            ContentId = contentId,
        });
        return contentId.ToString();
    }

    public async Task<bool> CheckReviewUser(CheckReviewUser request)
    {
        var process = await _db.Processes
            .Where(q => q.Request.Seller.OwnerId == request.AuthorId &&
                        q.Request.RequestNavigation.AuthorId == request.UserId &&
                        (q.Status == Enum.ProcessStatuses.COMPLETED ||
                         q.Status == Enum.ProcessStatuses.CANCELLED)).AnyAsync();
        return process;
    }

    public async Task<bool> CheckReviewSeller(CheckReviewSeller request)
    {
        var process = await _db.Processes
            .Where(q =>
                q.Request.SellerId == request.SellerId &&
                q.Request.RequestNavigation.AuthorId == request.AuthorId &&
                (q.Status == Enum.ProcessStatuses.COMPLETED ||
                 q.Status == Enum.ProcessStatuses.CANCELLED)).AnyAsync();
        return process;
    }
}