using Microsoft.AspNetCore.Mvc;
using API_FORMAT.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace API_FORMAT.Controllers
{
    [ApiController]
    [Route("topics")]
    public class TopicController : ControllerBase
    {
        private readonly AppDbContext _context;

        public TopicController(AppDbContext context)
        {
            _context = context;
        }

        // GET /topics
        [HttpGet]
        public async Task<IActionResult> GetAllTopics()
        {
            var topics = await _context.Topics
                .AsNoTracking()
                .ToListAsync();

            return Ok(topics);
        }

        // GET /topics/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetTopicById(int id)
        {
            var topic = await _context.Topics
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (topic == null)
                return NotFound();

            return Ok(topic);
        }

        // GET /topics/by-name?name={name}
        [HttpGet("by-name")]
        public async Task<IActionResult> GetTopicByName([FromQuery] string name)
        {
            var topic = await _context.Topics
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Name == name);

            if (topic == null)
                return NotFound();

            return Ok(topic);
        }

        // GET /topics/{id}/with-posts
        [HttpGet("{id}/with-posts")]
        public async Task<IActionResult> GetTopicWithPosts(int id)
        {
            var topic = await _context.Topics
                .Include(t => t.Posts)  // Подгружаем связанные посты
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == id);

            if (topic == null)
                return NotFound();

            return Ok(topic);
        }
    }
}