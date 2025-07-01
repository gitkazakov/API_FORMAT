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
        [Consumes("multipart/form-data")] // Явно указываем тип контента
        public async Task<IActionResult> CreatePost(int userId, [FromForm] PostCreateDto postDto)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists) return NotFound("User not found.");

            var post = new Post
            {
                Content = postDto.Content,
                CreatedAt = DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc),
                AuthorId = userId,
                CommunityId = postDto.CommunityId,
                TopicId = postDto.TopicId,
                ShareUrl = postDto.ShareUrl
            };

 
            if (postDto.ImageFile != null && postDto.ImageFile.Length > 0)
            {
  
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(postDto.ImageFile.FileName).ToLowerInvariant();

                if (!allowedExtensions.Contains(extension))
                {
                    return BadRequest("Недопустимый формат изображения. Разрешены только JPG, JPEG, PNG и GIF.");
                }

                if (postDto.ImageFile.Length > 5 * 1024 * 1024)
                {
                    return BadRequest("Размер изображения не должен превышать 5MB.");
                }


                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var uniqueFileName = Guid.NewGuid().ToString() + "_" + postDto.ImageFile.FileName;
                var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await postDto.ImageFile.CopyToAsync(fileStream);
                }

                post.MediaUrl = $"/uploads/{uniqueFileName}";
            }

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

        // GET /posts
        //[HttpGet]
        //public async Task<IActionResult> GetAllPosts(
        //    [FromQuery] int? communityId = null,
        //    [FromQuery] int? topicId = null,
        //    [FromQuery] int? authorId = null)
        //{
        //    var query = _context.Posts.AsQueryable();

        //    if (communityId.HasValue)
        //        query = query.Where(p => p.CommunityId == communityId);

        //    if (topicId.HasValue)
        //        query = query.Where(p => p.TopicId == topicId);

        //    if (authorId.HasValue)
        //        query = query.Where(p => p.AuthorId == authorId);

        //    var posts = await query
        //        .Include(p => p.Comments)
        //        .Include(p => p.Likes)
        //        .Include(p => p.Community)
        //        .Include(p => p.Topic)
        //        .OrderByDescending(p => p.CreatedAt)
        //        .ToListAsync();

        //    return Ok(posts);
        //}

        // GET /posts/{postId}
        [HttpGet("{postId}")]
        public async Task<IActionResult> GetPostById(int postId)
        {
            var post = await _context.Posts
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .Include(p => p.Community)
                .Include(p => p.Topic)
                .Select(p => new {
                    p.Id,
                    p.Content,
                    p.MediaUrl,
                    p.CreatedAt,
                    p.AuthorId,
                    Community = p.Community != null ? new { p.Community.Id, p.Community.Name } : null,
                    Topic = p.Topic != null ? new { p.Topic.Id, p.Topic.Name } : null,
                    CommentsCount = p.Comments.Count,
                    LikesCount = p.Likes.Count
                })
                .FirstOrDefaultAsync(p => p.Id == postId);

            if (post == null) return NotFound("Post not found.");

            return Ok(post);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllPosts(
            [FromQuery] int? communityId = null,
            [FromQuery] int? topicId = null,
            [FromQuery] int? authorId = null)
        {
            var query = _context.Posts.AsQueryable();

            if (communityId.HasValue)
                query = query.Where(p => p.CommunityId == communityId);

            if (topicId.HasValue)
                query = query.Where(p => p.TopicId == topicId);

            if (authorId.HasValue)
                query = query.Where(p => p.AuthorId == authorId);

            var posts = await query
                .Include(p => p.Comments)
                .Include(p => p.Likes)
                .Include(p => p.Community)
                .Include(p => p.Topic)
                .Select(p => new {
                    p.Id,
                    p.Content,
                    p.MediaUrl,
                    p.CreatedAt,
                    p.AuthorId,
                    Community = p.Community != null ? new { p.Community.Id, p.Community.Name } : null,
                    Topic = p.Topic != null ? new { p.Topic.Id, p.Topic.Name } : null,
                    CommentsCount = p.Comments.Count,
                    LikesCount = p.Likes.Count
                })
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return Ok(posts);
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

        [FromForm(Name = "ImageFile")]
        public IFormFile? ImageFile { get; set; }

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