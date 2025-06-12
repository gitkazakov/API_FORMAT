using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using API_FORMAT.Models;
using System.Linq;
using System.Threading.Tasks;

namespace API_FORMAT.Controllers
{
    [ApiController]
    [Route("users/{userId}/subscriptions")]
    public class UserSubscriptionsController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UserSubscriptionsController(AppDbContext context)
        {
            _context = context;
        }

        // GET /users/{userId}/subscriptions
        [HttpGet]
        public async Task<IActionResult> GetSubscriptions(int userId)
        {
            var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
            if (!userExists) return NotFound("User not found.");

            var subscriptions = await _context.Subscriptions
                .Where(s => s.UserId == userId)
                .Include(s => s.Community)
                .Select(s => new
                {
                    s.CommunityId,
                    CommunityName = s.Community!.Name,
                    CommunityDescription = s.Community.Description
                })
                .ToListAsync();

            return Ok(subscriptions);
        }

        // POST /users/{userId}/subscriptions
        [HttpPost]
        public async Task<IActionResult> Subscribe(int userId, [FromBody] SubscribeDto dto)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null || currentUserId != userId)
                return Forbid("You can only manage your own subscriptions.");

            var communityExists = await _context.Communities.AnyAsync(c => c.Id == dto.CommunityId);
            if (!communityExists)
                return NotFound("Community not found.");

            var subscriptionExists = await _context.Subscriptions.AnyAsync(s => s.UserId == userId && s.CommunityId == dto.CommunityId);
            if (subscriptionExists)
                return BadRequest("Already subscribed to this community.");

            var subscription = new Subscription
            {
                UserId = userId,
                CommunityId = dto.CommunityId
            };

            _context.Subscriptions.Add(subscription);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetSubscriptions), new { userId = userId }, subscription);
        }

        // DELETE /users/{userId}/subscriptions/{communityId}
        [HttpDelete("{communityId}")]
        public async Task<IActionResult> Unsubscribe(int userId, int communityId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == null || currentUserId != userId)
                return Forbid("You can only manage your own subscriptions.");

            var subscription = await _context.Subscriptions.SingleOrDefaultAsync(s => s.UserId == userId && s.CommunityId == communityId);
            if (subscription == null)
                return NotFound("Subscription not found.");

            _context.Subscriptions.Remove(subscription);
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

    public class SubscribeDto
    {
        public int CommunityId { get; set; }
    }
}
