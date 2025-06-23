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

        // Получить количество лайков поста
        [HttpGet("count")]
        public async Task<IActionResult> GetLikesCount(int postId)
        {
            var count = await _context.Likes
                .CountAsync(l => l.PostId == postId);

            return Ok(new { count });
        }

        // Проверить, лайкнул ли текущий пользователь пост
        [HttpGet("check/{userId}")]
        public async Task<IActionResult> CheckUserLike(int postId, int userId)
        {
            var hasLike = await _context.Likes
                .AnyAsync(l => l.PostId == postId && l.UserId == userId);

            return Ok(new { hasLike });
        }

        // Добавить лайк
        [HttpPost]
        public async Task<IActionResult> AddLike(int postId, [FromBody] int userId)
        {
            // Проверяем, существует ли пост
            var postExists = await _context.Posts.AnyAsync(p => p.Id == postId);
            if (!postExists) return NotFound("Post not found");

            // Проверяем, не лайкнул ли уже пользователь
            var alreadyLiked = await _context.Likes
                .AnyAsync(l => l.PostId == postId && l.UserId == userId);
            if (alreadyLiked) return BadRequest("User already liked this post");

            // Добавляем лайк
            var like = new Like { PostId = postId, UserId = userId };
            _context.Likes.Add(like);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // Удалить лайк
        [HttpDelete("{userId}")]
        public async Task<IActionResult> RemoveLike(int postId, int userId)
        {
            var like = await _context.Likes
                .FirstOrDefaultAsync(l => l.PostId == postId && l.UserId == userId);

            if (like == null) return NotFound("Like not found");

            _context.Likes.Remove(like);
            await _context.SaveChangesAsync();

            return Ok();
        }
    }
}
