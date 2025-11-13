using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using catalogoProductos.Application.Dto;
using catalogoProductos.Application.Interfaces;
using catalogoProductos.Domain.Entities;
using catalogoProductos.Domain.Interfaces;

namespace catalogoProductos.Application.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IConfiguration _configuration;

        public AuthService(IUserRepository userRepo, IConfiguration configuration)
        {
            _userRepo = userRepo;
            _configuration = configuration;
        }

        // ---------------------------------------------
        // REGISTRO DE USUARIO
        // ---------------------------------------------
        public async Task<UserDto> RegisterAsync(RegisterDto dto)
        {
            // Validar duplicado por username o email
            var existingUser = await _userRepo.GetByUserNameAsync(dto.UserName);
            var existingEmail = await _userRepo.GetByEmailAsync(dto.Email);

            if (existingUser != null || existingEmail != null)
                throw new InvalidOperationException("El usuario o el correo ya existen.");

            // Hashear contraseña
            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Determinar rol: si el email contiene 'admin', será Admin
            var role = dto.Email.Contains("admin", StringComparison.OrdinalIgnoreCase)
                ? Role.Admin
                : Role.User;

            // Crear la entidad
            var user = new User
            {
                UserName = dto.UserName,
                Email = dto.Email,
                Password = hashedPassword,
                Role = role
            };

            // Guardar en la base de datos
            await _userRepo.AddAsync(user);
            await _userRepo.SaveChangesAsync();

            // Retornar DTO sin contraseña
            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role.ToString()
            };
        }

        // ---------------------------------------------
        // LOGIN
        // ---------------------------------------------
        public async Task<string?> LoginAsync(LoginDto dto)
        {
            // Buscar usuario por username o email
            var user = await _userRepo.GetByUserNameAsync(dto.UserName)
                       ?? await _userRepo.GetByEmailAsync(dto.UserName);

            if (user == null)
                return null;

            // Verificar contraseña
            if (!BCrypt.Net.BCrypt.Verify(dto.Password, user.Password))
                return null;

            // Generar el JWT
            return GenerateJwtToken(user);
        }

        // ---------------------------------------------
        // GENERAR TOKEN JWT
        // ---------------------------------------------
        private string GenerateJwtToken(User user)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection["Key"] ?? throw new InvalidOperationException("JWT Key not configured");
            var issuer = jwtSection["Issuer"] ?? "catalogoApi";
            var audience = jwtSection["Audience"] ?? "catalogoClient";
            var duration = int.TryParse(jwtSection["DurationInMinutes"], out var d) ? d : 60;

            var keyBytes = Encoding.UTF8.GetBytes(key);
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddMinutes(duration),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(keyBytes),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }
}
