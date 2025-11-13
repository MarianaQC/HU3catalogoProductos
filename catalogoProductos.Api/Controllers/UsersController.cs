using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using catalogoProductos.Domain.Interfaces;
using catalogoProductos.Domain.Entities;
using catalogoProductos.Application.Dto;

namespace catalogoProductos.Api.Controllers
{
    // Controller para manejo de usuarios
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly IUserRepository _userRepo;

        // Inyectamos el repositorio de usuarios
        public UsersController(IUserRepository userRepo)
        {
            _userRepo = userRepo;
        }

        // GET: /api/users
        // Solo Admin puede listar todos los usuarios
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetAll()
        {
            var users = await _userRepo.GetAllAsync();

            // Mapear a UserDto (no incluir password)
            var result = users.Select(u => new UserDto
            {
                Id = u.Id,
                UserName = u.UserName,
                Email = u.Email,
                Role = u.Role.ToString()
            });

            return Ok(result);
        }

        // GET: /api/users/{id}
        // Admin o el mismo usuario puede ver su info
        [HttpGet("{id:int}")]
        [Authorize]
        public async Task<IActionResult> GetById(int id)
        {
            // obtener id del usuario logueado desde el token
            var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var callerRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(claimId))
                return Unauthorized("No se encontró el identificador del usuario en el token.");

            // intentar convertir el claim a entero solo si es un número
            int callerId = 0;
            int.TryParse(claimId, out callerId);

            // Si no es admin y no coincide el id (en caso de tener id numérico) ni el email (en caso de tener email)
            bool isSameUser = callerId == id || claimId.Equals(id.ToString(), StringComparison.OrdinalIgnoreCase);

            if (callerRole != "Admin" && !isSameUser)
                return Forbid();

            var user = await _userRepo.GetByIdAsync(id);
            if (user == null)
                return NotFound();

            var dto = new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                Email = user.Email,
                Role = user.Role.ToString()
            };

            return Ok(dto);
        }


        // PUT: /api/users/{id}
        // Admin o el mismo usuario puede actualizar (no se actualiza password aquí)
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] UserDto update)
        {
            var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (claimId == null) return Unauthorized();

            var callerId = int.Parse(claimId);
            var callerRole = User.FindFirst(ClaimTypes.Role)?.Value;

            if (callerRole != "Admin" && callerId != id) return Forbid();

            var existing = await _userRepo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            // Solo permitimos actualizar username, email y role (si es admin)
            existing.UserName = update.UserName;
            existing.Email = update.Email;

            if (callerRole == "Admin")
            {
                // El admin puede cambiar el role (string -> enum)
                if (Enum.TryParse<Role>(update.Role, out var newRole))
                {
                    existing.Role = newRole;
                }
            }

            await _userRepo.UpdateAsync(existing);

            var dto = new UserDto
            {
                Id = existing.Id,
                UserName = existing.UserName,
                Email = existing.Email,
                Role = existing.Role.ToString()
            };

            return Ok(dto);
        }

        // DELETE: /api/users/{id}
        // Solo Admin puede eliminar usuarios
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _userRepo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            await _userRepo.DeleteAsync(existing);
            return NoContent();
        }
    }
}
