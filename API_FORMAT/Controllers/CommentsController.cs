using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_FORMAT.Models;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace API_FORMAT.Controllers
{
    [ApiController]
    [Route("users/{userId}/comments")]
    public class UserCommentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserCommentsController(AppDbContext context)
        {
            _context = context;
        }

        // GET /users/{userId}/comments
        [HttpGet]
        public async Task<IActionResult> GetUserComments(int userId)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists) return NotFound("User not found.");

            var comments = await _context.Comments
                .Where(c => c.UserId == userId)
                .Include(c => c.Post)
                .ThenInclude(p => p.Author)
                .Select(c => new
                {
                    c.Id,
                    c.CommentText,
                    c.CreatedAt,
                    PostId = c.PostId,
                    PostContent = c.Post!.Content,
                    PostAuthorId = c.Post.AuthorId,
                    PostAuthorLogin = c.Post.Author!.Login
                })
                .ToListAsync();

            return Ok(comments);
        }
    }

    [ApiController]
    [Route("posts/{postId}/comments")]
    public class PostCommentsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public PostCommentsController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetPostComments(int postId)
        {
            var postExists = await _context.Posts.AnyAsync(p => p.Id == postId);
            if (!postExists) return NotFound("Post not found.");

            var comments = await _context.Comments
                .Where(c => c.PostId == postId)
                .Include(c => c.User) 
                .OrderByDescending(c => c.CreatedAt)  
                .Select(c => new
                {
                    c.Id,
                    c.CommentText,
                    c.CreatedAt,
                    UserId = c.User.Id,
                    UserLogin = c.User.Login,
                    UserAvatarUrl = c.User.AvatarUrl
                })
                .ToListAsync();

            return Ok(comments);
        }

        [HttpGet("count")]
        public async Task<IActionResult> GetPostCommentsCount(int postId)
        {
            var postExists = await _context.Posts.AnyAsync(p => p.Id == postId);
            if (!postExists) return NotFound("Post not found.");

            var count = await _context.Comments
                .CountAsync(c => c.PostId == postId);

            return Ok(new { Count = count });
        }


        // POST /posts/{postId}/comments
        [HttpPost]
        public async Task<IActionResult> CreateComment(int postId, [FromBody] CommentCreateDto dto)
        {    

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null)
                return Unauthorized("User ID header missing or invalid.");
   
            if (string.IsNullOrWhiteSpace(dto.CommentText))
                return BadRequest("Comment text is required");

            var postExists = await _context.Posts.AnyAsync(p => p.Id == postId);
            if (!postExists)
                return NotFound("Post not found");

            var comment = new Comment
            {
                PostId = postId,
                UserId = currentUserId.Value,
                CommentText = dto.CommentText,
                CreatedAt = DateTime.Now 
            };

            _context.Comments.Add(comment);
            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(currentUserId.Value);

            return Ok(new
            {
                comment.Id,
                comment.CommentText,
                comment.CreatedAt,
                UserLogin = user?.Login,
                UserAvatarUrl = user?.AvatarUrl
            });
        }



        // PUT /comments/{commentId}
        [HttpPut("/comments/{commentId}")]
        public async Task<IActionResult> UpdateComment(int commentId, [FromBody] CommentUpdateDto dto)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
                return NotFound("Comment not found.");

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null || comment.UserId != currentUserId)
                return Forbid("You are not the author of this comment.");

            comment.CommentText = dto.CommentText ?? comment.CommentText;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE /comments/{commentId}
        [HttpDelete("/comments/{commentId}")]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var comment = await _context.Comments.FindAsync(commentId);
            if (comment == null)
                return NotFound("Comment not found.");

            var currentUserId = GetCurrentUserId();
            if (currentUserId == null || comment.UserId != currentUserId)
                return Forbid("You are not the author of this comment.");

            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [NonAction]
        public async Task<IActionResult> GetComment(int commentId)
        {
            var comment = await _context.Comments
                .Include(c => c.Post)
                .Include(c => c.User)
                .SingleOrDefaultAsync(c => c.Id == commentId);

            if (comment == null) return NotFound();

            return Ok(comment);
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

    public class CommentCreateDto
    {
        public string CommentText { get; set; } = null!;
    }

    public class CommentUpdateDto
    {
        public string? CommentText { get; set; }
    }
}
