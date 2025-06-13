using Microsoft.AspNetCore.Mvc;
using API_FORMAT.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace API_FORMAT.Controllers
{
    [ApiController]
    [Route("communities")]
    public class CommunitiesController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CommunitiesController(AppDbContext context)
        {
            _context = context;
        }

        // GET /communities
        [HttpGet]
        public async Task<IActionResult> GetAllCommunities()
        {
            var communities = await _context.Communities
                .AsNoTracking()
                .ToListAsync();

            return Ok(communities);
        }

        // GET /communities/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetCommunityById(int id)
        {
            var community = await _context.Communities
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (community == null)
                return NotFound();

            return Ok(community);
        }

        // GET /communities/by-name?name={name}
        [HttpGet("by-name")]
        public async Task<IActionResult> GetCommunityByName([FromQuery] string name)
        {
            var community = await _context.Communities
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == name);

            if (community == null)
                return NotFound();

            return Ok(community);
        }

        // GET /communities/{id}/with-posts
        [HttpGet("{id}/with-posts")]
        public async Task<IActionResult> GetCommunityWithPosts(int id)
        {
            var community = await _context.Communities
                .Include(c => c.Posts)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (community == null)
                return NotFound();

            return Ok(community);
        }

        // GET /communities/{id}/with-subscriptions
        [HttpGet("{id}/with-subscriptions")]
        public async Task<IActionResult> GetCommunityWithSubscriptions(int id)
        {
            var community = await _context.Communities
                .Include(c => c.Subscriptions)
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == id);

            if (community == null)
                return NotFound();

            return Ok(community);
        }

        // GET /communities/most-popular
        [HttpGet("most-popular")]
        public async Task<IActionResult> GetMostPopularCommunities([FromQuery] int count = 5)
        {
            var communities = await _context.Communities
                .OrderByDescending(c => c.PublicationCount ?? 0)
                .Take(count)
                .AsNoTracking()
                .ToListAsync();

            return Ok(communities);
        }
    }
}