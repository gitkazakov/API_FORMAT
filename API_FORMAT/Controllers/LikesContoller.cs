using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_FORMAT.Models;
using System.Linq;
using System.Threading.Tasks;

namespace API_FORMAT.Controllers
{
    [ApiController]
    [Route("users/{userId}/likes")]
    public class UserLikesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserLikesController(AppDbContext context)
        {
            _context = context;
        }

        // GET /users/{userId}/likes
        [HttpGet]
        public async Task<IActionResult> GetUserLikes(int userId)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists) return NotFound("User not found.");

            var likedPosts = await _context.Likes
                .Where(l => l.UserId == userId)
                .Include(l => l.Post)
                .ThenInclude(p => p.Author)
                .Select(l => new
                {
                    PostId = l.PostId,
                    Content = l.Post!.Content,
                    AuthorId = l.Post.AuthorId,
                    AuthorLogin = l.Post.Author!.Login,
                    l.Post.MediaUrl,
                    l.Post.CreatedAt
                })
                .ToListAsync();

            return Ok(likedPosts);
        }
    }

    [ApiController]
    [Route("posts/{postId}/likes")]
    public class PostLikesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PostLikesController(AppDbContext context)
        {
            _context = context;
        }

        // POST /posts/{postId}/likes
        [HttpPost]
        public async Task<IActionResult> AddLike(int postId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return BadRequest("User ID header missing or invalid.");

            var postExists = await _context.Posts.AnyAsync(p => p.Id == postId);
            if (!postExists)
                return NotFound("Post not found.");

            var likeExists = await _context.Likes.AnyAsync(l => l.PostId == postId && l.UserId == currentUserId);
            if (likeExists)
                return BadRequest("Like already exists.");

            var like = new Like
            {
                PostId = postId,
                UserId = currentUserId
            };

            _context.Likes.Add(like);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetLike), new { postId = postId }, like);
        }

        // DELETE /posts/{postId}/likes
        [HttpDelete]
        public async Task<IActionResult> RemoveLike(int postId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return BadRequest("User ID header missing or invalid.");

            var like = await _context.Likes.SingleOrDefaultAsync(l => l.PostId == postId && l.UserId == currentUserId);
            if (like == null)
                return NotFound("Like not found.");

            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();

            return NoContent();
        }


        [NonAction]
        public async Task<IActionResult> GetLike(int postId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return BadRequest();

            var like = await _context.Likes.SingleOrDefaultAsync(l => l.PostId == postId && l.UserId == currentUserId);
            if (like == null)
                return NotFound();

            return Ok(like);
        }

        private int? GetCurrentUserId()
        {
            if (Request.Headers.TryGetValue("X-User-Id", out var userIdValues))
            {
                if (int.TryParse(userIdValues.FirstOrDefault(), out int userId))
                    return userId;
            }
            return null;
        }
    }
}
