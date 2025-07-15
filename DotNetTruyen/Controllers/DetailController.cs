
using DotNetTruyen.Data;
using DotNetTruyen.Hubs;
using DotNetTruyen.Models;
using DotNetTruyen.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Xml.Linq;

namespace DotNetTruyen.Controllers
{
    public class DetailController : Controller
    {
        private readonly DotNetTruyenDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly ICompositeViewEngine _viewEngine;
        private readonly IHubContext<CommentHub> _hubContext;
        private readonly UserService _userService;
        public DetailController(DotNetTruyenDbContext context, UserManager<User> userManager, ICompositeViewEngine viewEngine, IHubContext<CommentHub> hubContext,UserService userService)
        {
            _context = context;
            _userManager = userManager;
            _viewEngine = viewEngine;
            _hubContext = hubContext;
            _userService = userService;
        }

        public async Task<IActionResult> Index(Guid id, int page = 1, string sortOrder = "desc")
        {
            int pageSizeChapter = 5;
            var comic = await _context.Comics
                .Include(c => c.Chapters)
                .Include(c => c.ComicGenres)
                    .ThenInclude(cg => cg.Genre)
                .Include(c => c.Follows)
                .Include(c => c.Likes)
                .FirstOrDefaultAsync(c => c.Id == id);
            var chaptersQuery = comic.Chapters
               .Where(c => c.IsPublished && c.DeletedAt == null);

            // Sắp xếp theo sortOrder
            var orderedChapters = sortOrder == "asc"
                ? chaptersQuery.OrderBy(c => c.ChapterNumber).ToList()
                : chaptersQuery.OrderByDescending(c => c.ChapterNumber).ToList();

            // Phân trang
            int totalChapters = orderedChapters.Count;
            int totalPages = (int)Math.Ceiling(totalChapters / (double)pageSizeChapter);

            var pagedChapters = orderedChapters
                .Skip((page - 1) * pageSizeChapter)
                .Take(pageSizeChapter)
                .ToList();
            if (comic == null)
                return NotFound();
            if (User.Identity.IsAuthenticated)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.IsFollowing = _context.Follows.Any(f => f.ComicId == id && f.UserId == Guid.Parse(userId));
                ViewBag.IsLiked = _context.Likes.Any(l => l.ComicId == id && l.UserId == Guid.Parse(userId));
            }
            else
            {
                ViewBag.IsFollowing = false;
                ViewBag.IsLiked = false;
            }
            ViewBag.Comics = _context.Comics
             .Where(c => c.Id != id)
             .OrderBy(c => Guid.NewGuid())
            .Take(10)
         .ToList();

            const int pageSize = 5;
            var topLevelComments = await _context.Comments
                .Include(c => c.User)
                .Where(c => c.ComicId == id && c.CommentId == null)
                .OrderByDescending(c => c.Date)
                .Take(pageSize)
                .ToListAsync();

            // Calculate the total number of top-level comments for pagination
            var totalTopLevelComments = await _context.Comments
                .CountAsync(c => c.ComicId == id && c.CommentId == null);

            foreach (var comment in topLevelComments)
            {
                comment.ReplyCount = await _context.Comments.CountAsync(c => c.CommentId == comment.Id);
                comment.UserLevel = await _userService.GetUserLevelNameAsync(_context, comment.UserId);
            }

            ViewBag.Comments = topLevelComments;
            ViewBag.TotalComments = totalTopLevelComments;
            ViewBag.PageSize = pageSize;
            ViewBag.ComicId = id;

            // Truyền thông tin phân trang và sortOrder vào ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalChapters = totalChapters;
            ViewBag.PageSizeChapter = pageSizeChapter;
            ViewBag.SortOrder = sortOrder;

            // Gán danh sách chương đã phân trang vào Model.Chapters
            comic.Chapters = pagedChapters;
            return View(comic);
        }

        [HttpGet]
        public async Task<IActionResult> GetMoreComments(Guid comicId, int skip)
        {
            const int pageSize = 5;

            // Fetch the next batch of top-level comments
            var comments =await _context.Comments
                .Include(c => c.User)
                .Where(c => c.ComicId == comicId && c.CommentId == null)
                .OrderByDescending(c => c.Date)
                .Skip(skip)
                .Take(pageSize)
                .ToListAsync();

            foreach (var comment in comments)
            {
                comment.ReplyCount = await  _context.Comments.CountAsync(c => c.CommentId == comment.Id);
                comment.UserLevel = await _userService.GetUserLevelNameAsync(_context, comment.UserId);
            }

            // Return the comments as a partial view
            return PartialView("_CommentPartial", comments);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Comment(Guid comicId, string content, Guid? parentId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để bình luận." });
            }

            var comment = new Comment
            {
                Content = content,
                Date = DateTime.Now,
                UserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)),
                ComicId = comicId,
                CommentId = parentId,
                ReplyCount = 0, // New comment has no replies initially
                UserLevel = await _userService.GetUserLevelNameAsync(_context, Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)))
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            // Broadcast the reload message to all clients
            await _hubContext.Clients.Group($"Comic_{comicId}").SendAsync("ReloadComments", comicId);

            return Json(new { success = true });
        }

        // Helper method to render a partial view to a string
        private async Task<string> RenderPartialViewToString(string viewName, object model)
        {
            ViewData.Model = model;
            using (var sw = new StringWriter())
            {
                var viewResult = _viewEngine.FindView(ControllerContext, viewName, false);
                var viewContext = new ViewContext(ControllerContext, viewResult.View, ViewData, TempData, sw, new HtmlHelperOptions());
                await viewResult.View.RenderAsync(viewContext);
                return sw.GetStringBuilder().ToString();
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetReplies(Guid commentId)
        {
            // Fetch replies for the given commentId
            var replies = await _context.Comments
                .Include(c => c.User)
                .Include(c => c.Comments) // Include nested replies if needed
                .ThenInclude(r => r.User)
                .Where(c => c.CommentId == commentId)
                .OrderByDescending(c => c.Date)
                .ToListAsync();

            foreach (var comment in replies)
            {
                comment.ReplyCount = await _context.Comments.CountAsync(c => c.CommentId == comment.Id);
                comment.UserLevel = await _userService.GetUserLevelNameAsync(_context, comment.UserId);
            }

            // Return the replies as a partial view
            return PartialView("_CommentPartial", replies);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> EditComment(Guid commentId, string content)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            var comment = _context.Comments
                .Include(c => c.User)
                .FirstOrDefault(c => c.Id == commentId);

            if (comment == null)
            {
                return NotFound();
            }

            // Check if the current user is the author of the comment
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (comment.UserId.ToString() != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            // Update the comment content
            comment.Content = content;
            comment.Date = DateTime.Now; // Update the timestamp (optional)
            await _context.SaveChangesAsync();

            // Broadcast the reload message to all clients
            await _hubContext.Clients.Group($"Comic_{comment.ComicId}").SendAsync("ReloadComments", comment.ComicId);

            return Json(new { success = true });
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> DeleteComment(Guid commentId)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Unauthorized();
            }

            // Lấy comment để kiểm tra quyền
            var comment = await _context.Comments
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
            {
                return NotFound();
            }

            // Kiểm tra quyền xóa
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (comment.UserId.ToString() != currentUserId && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var comicId = comment.ComicId;

            // Xóa tất cả comment con bằng SQL an toàn hơn
            await _context.Database.ExecuteSqlInterpolatedAsync($@"
        WITH CommentHierarchy AS (
            SELECT Id FROM Comments WHERE Id = {commentId}
            UNION ALL
            SELECT c.Id FROM Comments c
            INNER JOIN CommentHierarchy ch ON c.ReplyId = ch.Id
        )
        DELETE FROM Comments WHERE Id IN (SELECT Id FROM CommentHierarchy);");
            // Gửi tín hiệu reload đến tất cả client
            await _hubContext.Clients.Group($"Comic_{comicId}").SendAsync("ReloadComments", comicId);

            return Json(new { success = true });
        }
        [HttpGet]
        public async Task<IActionResult> GetComments(Guid comicId, int skip = 0)
        {
            const int pageSize = 5;

            var comments = _context.Comments
                .Include(c => c.User)
                .Where(c => c.ComicId == comicId && c.CommentId == null)
                .OrderByDescending(c => c.Date)
                .Skip(skip)
                .Take(pageSize)
                .ToList();

            var totalTopLevelComments = _context.Comments
                .Count(c => c.ComicId == comicId && c.CommentId == null);

            foreach (var comment in comments)
            {
                comment.ReplyCount = _context.Comments.Count(c => c.CommentId == comment.Id);
                comment.UserLevel = await _userService.GetUserLevelNameAsync(_context, comment.UserId);
            }

            return Json(new
            {
                html = RenderPartialViewToString("_CommentPartial", comments),
                totalComments = totalTopLevelComments
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleFollow([FromBody] FollowRequestModel request)
        {
            try
            {
                Console.WriteLine($"Received Comic ID: {request.Id}");
                if (!User.Identity.IsAuthenticated)
                {
                    return Unauthorized(new { success = false, message = "Vui lòng đăng nhập" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Console.WriteLine($"User ID: {userId}");
                if (!Guid.TryParse(userId, out Guid parsedUserId))
                {
                    return Json(new { success = false, message = "Không thể xác định người dùng" });
                }

                // Kiểm tra Comic tồn tại
                var comic = _context.Comics.FirstOrDefault(c => c.Id == request.Id);
                if (comic == null)
                {
                    Console.WriteLine($"Comic with ID {request.Id} not found.");
                    return Json(new { success = false, message = "Không tìm thấy truyện" });
                }

                var follow = _context.Follows
                    .FirstOrDefault(f => f.ComicId == request.Id && f.UserId == parsedUserId);

                bool wasFollowing = follow != null;

                if (!wasFollowing)
                {
                    follow = new Follow
                    {
                        ComicId = request.Id,
                        UserId = parsedUserId,
                    };
                    _context.Follows.Add(follow);
                }
                else
                {
                    _context.Follows.Remove(follow);
                }

                _context.SaveChanges();

                var followCount = _context.Follows.Count(f => f.ComicId == request.Id);
                var likeCount = _context.Likes.Count(l => l.ComicId == request.Id);

                
                await _hubContext.Clients.All.SendAsync("ReceiveComicUpdate", request.Id, followCount, likeCount,!wasFollowing,null);

                return Json(new
                {
                    success = true,
                    isFollowing = !wasFollowing,
                    followCount = followCount
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ToggleFollow: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLike([FromBody] LikeRequestModel request)
        {
            try
            {
                Console.WriteLine($"Received Comic ID for Like: {request.Id}");
                if (!User.Identity.IsAuthenticated)
                {
                    return Unauthorized(new { success = false, message = "Vui lòng đăng nhập" });
                }

                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                Console.WriteLine($"User ID: {userId}");
                if (!Guid.TryParse(userId, out Guid parsedUserId))
                {
                    return Json(new { success = false, message = "Không thể xác định người dùng" });
                }

                var comic = _context.Comics.FirstOrDefault(c => c.Id == request.Id);
                if (comic == null)
                {
                    Console.WriteLine($"Comic with ID {request.Id} not found.");
                    return Json(new { success = false, message = "Không tìm thấy truyện" });
                }

                var like = _context.Likes
                    .FirstOrDefault(l => l.ComicId == request.Id && l.UserId == parsedUserId);

                bool wasLiked = like != null;

                if (!wasLiked)
                {
                    like = new Like
                    {
                        ComicId = request.Id,
                        UserId = parsedUserId,
                    };
                    _context.Likes.Add(like);
                }
                else
                {
                    _context.Likes.Remove(like);
                }

                _context.SaveChanges();

                var followCount = _context.Follows.Count(f => f.ComicId == request.Id);
                var likeCount = _context.Likes.Count(l => l.ComicId == request.Id);


                await _hubContext.Clients.All.SendAsync("ReceiveComicUpdate", request.Id, followCount, likeCount,null,!wasLiked);
                return Json(new
                {
                    success = true,
                    isLiked = !wasLiked,
                    likeCount = likeCount
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ToggleLike: {ex.Message}\nStackTrace: {ex.StackTrace}");
                return Json(new { success = false, message = "Lỗi server: " + ex.Message });
            }
        }

        
            

        
    }
}
public class LikeRequestModel
{
    public Guid Id { get; set; }
}

// Tạo class request model
public class FollowRequestModel
{
    public Guid Id { get; set; }
}