using Microsoft.AspNetCore.Mvc;
using API_FORMAT.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace API_FORMAT.Controllers
{
    [ApiController]
    [Route("users")]
    public class UsersController : ControllerBase
    {
        private readonly AppDbContext _context;

        public UsersController(AppDbContext context)
        {
            _context = context;
        }

        // POST /users/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterDto dto)
        {
            if (await _context.Users.AnyAsync(u => u.Login == dto.Login || u.Email == dto.Email))
                return BadRequest(new { Message = "User with this login or email already exists" });

            var user = new User
            {
                Login = dto.Login,
                Email = dto.Email,
                Password = dto.Password,
                Phone = dto.Phone,
                RoleId = 2
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new
            {
                Message = "Registration successful",
                User = new
                {
                    user.Id,
                    user.Login,
                    user.Email,
                    user.Phone
                }
            });
        }
        // POST /users/login 
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginDto dto)
        {
            Console.WriteLine($"Login attempt: {dto.Login}");
            try
            {
                if (string.IsNullOrEmpty(dto.Login) || string.IsNullOrEmpty(dto.Password))
                    return BadRequest(new { Message = "Login and password are required" });

                var user = await _context.Users
                    .SingleOrDefaultAsync(u => u.Login == dto.Login && u.Password == dto.Password);

                if (user == null)
                    return Unauthorized(new { Message = "Invalid login or password" });

                return Ok(new
                {
                    Message = "Login successful",
                    User = new
                    {
                        user.Id,
                        user.Login,
                        user.Email,
                        user.Phone,
                        user.RoleId
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Internal server error", Details = ex.Message });
            }
        }

        // GET /users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id)
        {
            var user = await _context.Users.AsNoTracking().SingleOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            return Ok(new
            {
                user.Id,
                user.Login,
                user.Email,
                user.Phone,
                RoleId = user.RoleId
            });
        }

        // PUT /users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] UserUpdateDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            user.Email = dto.Email ?? user.Email;
            user.Phone = dto.Phone ?? user.Phone;
            if (!string.IsNullOrEmpty(dto.Password))
                user.Password = dto.Password;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE /users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST /users/{id}/change-password
        [HttpPost("{id}/change-password")]
        public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();

            if (user.Password != dto.OldPassword)
                return BadRequest("Old password is incorrect.");

            user.Password = dto.NewPassword;
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }


    public class UserRegisterDto
    {
        public string Login { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? Phone { get; set; }
    }

    public class UserLoginDto
    {
        public string Login { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class UserUpdateDto
    {
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Password { get; set; }
    }

    public class ChangePasswordDto
    {
        public string OldPassword { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }
}
