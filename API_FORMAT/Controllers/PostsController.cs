using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_FORMAT.Models;
using System.Linq;
using System.Threading.Tasks;

namespace API_FORMAT.Controllers
{
    [ApiController]
    [Route("users/{userId}/posts")]
    public class UserPostsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserPostsController(AppDbContext context)
        {
            _context = context;
        }

        // GET /users/{userId}/posts
        [HttpGet]
        public async Task<IActionResult> GetUserPosts(int userId)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists) return NotFound("User not found.");

            var posts = await _context.Posts
                .Where(p => p.AuthorId == userId)
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .Include(p => p.Community)
                .Include(p => p.Topic)
                .ToListAsync();

            return Ok(posts);
        }

        // POST /users/{userId}/posts
        [HttpPost]
        public async Task<IActionResult> CreatePost(int userId, [FromBody] PostCreateDto postDto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists) return NotFound("User not found.");

            var post = new Post
            {
                Content = postDto.Content,
                MediaUrl = postDto.MediaUrl,
                CreatedAt = DateTime.UtcNow,
                AuthorId = userId,
                CommunityId = postDto.CommunityId,
                TopicId = postDto.TopicId,
                ShareUrl = postDto.ShareUrl
            };

            _context.Posts.Add(post);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUserPosts), new { userId = userId }, post);
        }
    }

    [ApiController]
    [Route("posts")]
    public class PostsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PostsController(AppDbContext context)
        {
            _context = context;
        }

        // PUT /posts/{postId}
        [HttpPut("{postId}")]
        public async Task<IActionResult> UpdatePost(int postId, [FromBody] PostUpdateDto postDto)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound("Post not found.");

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null || post.AuthorId != currentUserId)
                return Forbid("You are not the author of this post.");

            post.Content = postDto.Content ?? post.Content;
            post.MediaUrl = postDto.MediaUrl ?? post.MediaUrl;
            post.CommunityId = postDto.CommunityId ?? post.CommunityId;
            post.TopicId = postDto.TopicId ?? post.TopicId;
            post.ShareUrl = postDto.ShareUrl ?? post.ShareUrl;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE /posts/{postId}
        [HttpDelete("{postId}")]
        public async Task<IActionResult> DeletePost(int postId)
        {
            var post = await _context.Posts.FindAsync(postId);
            if (post == null) return NotFound("Post not found.");

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null || post.AuthorId != currentUserId)
                return Forbid("You are not the author of this post.");

            _context.Posts.Remove(post);
            await _context.SaveChangesAsync();

            return NoContent();
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

    public class PostCreateDto
    {
        public string Content { get; set; } = null!;
        public string? MediaUrl { get; set; }
        public int? CommunityId { get; set; }
        public int? TopicId { get; set; }
        public string? ShareUrl { get; set; }
    }

    public class PostUpdateDto
    {
        public string? Content { get; set; }
        public string? MediaUrl { get; set; }
        public int? CommunityId { get; set; }
        public int? TopicId { get; set; }
        public string? ShareUrl { get; set; }
    }
}
