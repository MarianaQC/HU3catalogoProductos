using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using catalogoProductos.Domain.Entities;
using catalogoProductos.Domain.Interfaces;

namespace catalogoProductos.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _configuration;

        public AuthController(IUserRepository userRepo, IConfiguration configuration)
        {
            _userRepo = userRepo;
            _configuration = configuration;
        }

        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "El email y la contraseña son obligatorios." });

            // Verificar si el correo ya existe
            var existing = await _userRepo.GetByEmailAsync(request.Email);
            if (existing != null)
                return BadRequest(new { message = "El usuario ya existe." });

            // Asignar rol automáticamente: si contiene "admin" será Admin
            var role = request.Email.Contains("admin", StringComparison.OrdinalIgnoreCase)
                ? Role.Admin
                : Role.User;

            // Hashear la contraseña
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

            var user = new User
            {
                UserName = request.Name,
                Email = request.Email,
                Password = passwordHash,
                Role = role
            };

            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync(); // 🔹 Persistir cambios en la base real

            return Ok(new { message = $"Usuario registrado correctamente con rol {role}." });
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest(new { message = "El email y la contraseña son obligatorios." });

            var user = await _userRepo.GetByEmailAsync(request.Email);
            if (user == null)
                return Unauthorized(new { message = "Usuario no encontrado." });

            var validPassword = BCrypt.Net.BCrypt.Verify(request.Password, user.Password);
            if (!validPassword)
                return Unauthorized(new { message = "Contraseña incorrecta." });

            var token = GenerateJwtToken(user);
            return Ok(new
            {
                token,
                user = new
                {
                    user.Id,
                    user.UserName,
                    user.Email,
                    Role = user.Role.ToString()
                }
            });
        }

        // -------------------------------------
        // TOKEN JWT
        // -------------------------------------
        private string GenerateJwtToken(User user)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim("name", user.UserName),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString())
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(
                    double.Parse(_configuration["Jwt:DurationInMinutes"] ?? "60")
                ),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // Modelos auxiliares (solo para peticiones)
    public class UserRegisterRequest
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }

    public class UserLoginRequest
    {
        public string Email { get; set; } = "";
        public string Password { get; set; } = "";
    }
}