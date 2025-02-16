using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using SGK_Web_Backend.DbConnection;
using SGK_Web_Backend.Models;
using SGK_Web_Backend.Enums;

// NOTE: Content-Type must be set to "application/json", frontend folks should keep that in mind

namespace SGK_Web_Backend.Controllers;
[Route("api/[controller]")]
[ApiController]
public class UserController:ControllerBase
{   
    private readonly AppDbContext _context;

    public UserController(AppDbContext context)
    {
        _context = context;
    }

    public string HashPassword(string password, string salt)
    {
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            var hashedBytes = sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(password + salt));
            return BitConverter.ToString(hashedBytes).Replace("-", "").ToLower() + ":" + salt;
        }
    }

    public bool VerifyPassword(string password, string hashedPassword)
    {
        string[] parts = hashedPassword.Split(":");
        string salt = parts[1];

        return hashedPassword == HashPassword(password, salt);
    }

    [HttpPost("login")]
   public async Task<IActionResult> Login([FromBody] UserLoginDTO userDto)
    {
        User? dbUser = _context.users.FirstOrDefault(u => u.username == userDto.username);
        
        if (dbUser == null)
            return Unauthorized(new { message = "User not found!" });

        if (!VerifyPassword(userDto.password, dbUser.password))
            return Unauthorized(new { message = "Wrong password!" });

        // Create claims for the user
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, dbUser.username),
            new Claim(ClaimTypes.NameIdentifier, dbUser.id.ToString())
            // Add additional claims as needed (e.g., roles)
            // TODO: Deep dive into the usage of Claim class and look for some use cases 
        };

        var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        
        var authProperties = new AuthenticationProperties
        {
            IsPersistent = true, // Remember me functionality
            ExpiresUtc = DateTimeOffset.UtcNow.AddDays(1) // Cookie expiration time
        };

        await HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity),
            authProperties);

        return Ok(new { message = "Login successful" });
    }

    [HttpPost("register")]
    public IActionResult CreateUser([FromBody] User newUser)
    {
        if (_context.users.Any(u => u.username == newUser.username))
            return BadRequest(new { message = "User already exists!" });

        if (_context.users.Any(u => u.email == newUser.email))
            return BadRequest(new { message = "Email already exists!" });

        // * Please do not move this default assignation to User.cs since it will exploit default value manipulation!
        newUser.role = UserRole.User;
        newUser.verified = false; // Account creation validation for admins to confirm

        // Hash the password with a random salt for security
        newUser.password = HashPassword(newUser.password, 
            BCrypt.Net.BCrypt.GenerateSalt() // Generate a random salt
        );
        
        _context.users.Add(newUser);
        _context.SaveChanges();

        return Ok(new { message = "User created successfully" });
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return Ok(new { message = "Logged out successfully" });
    }

    [HttpGet("current")]
    public IActionResult GetCurrentUser()
    {
        if (!(User.Identity?.IsAuthenticated ?? false))
            return Unauthorized(new { message = "Not authenticated" });

        var username = User.Identity.Name;
        var user = _context.users.FirstOrDefault(u => u.username == username);
        
        if (user == null)
            return NotFound(new { message = "User not found" });

        return Ok(new { 
            username = user.username,
            id = user.id,
            role = user.role
            // ...
            // TODO: We will be adding more data to return when needed by frontend
        });
    }
    
}