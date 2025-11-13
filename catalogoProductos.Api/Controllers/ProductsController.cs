using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using catalogoProductos.Domain.Interfaces;
using catalogoProductos.Domain.Entities;
using catalogoProductos.Application.Dto;

namespace catalogoProductos.Api.Controllers
{
    // Controller para manejo de productos
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductRepository _productRepo;

        // Inyectamos repo de productos
        public ProductsController(IProductRepository productRepo)
        {
            _productRepo = productRepo;
        }

        // GET: /api/products
        // Listado público (AllowAnonymous)
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> GetAll()
        {
            var products = await _productRepo.GetAllAsync();
            var result = products.Select(p => new ProductDto
            {
                Id = p.Id,
                ProductName = p.ProductName,
                Code = p.Code,
                Stock = p.Stock,
                Price = p.Price
            });

            return Ok(result);
        }

        // GET: /api/products/{id}
        // Detalle público
        [HttpGet("{id:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetById(int id)
        {
            var product = await _productRepo.GetByIdAsync(id);
            if (product == null) return NotFound();

            var dto = new ProductDto
            {
                Id = product.Id,
                ProductName = product.ProductName,
                Code = product.Code,
                Stock = product.Stock,
                Price = product.Price
            };

            return Ok(dto);
        }

        // POST: /api/products
        // Crear producto: requiere estar autenticado (cualquiera logueado). Si quieres solo Admin, cambia Authorize(Roles="Admin")
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] ProductDto dto)
        {
            // Validaciones simples
            if (string.IsNullOrWhiteSpace(dto.ProductName) || string.IsNullOrWhiteSpace(dto.Code))
                return BadRequest("ProductName y Code son requeridos.");

            var product = new Product
            {
                ProductName = dto.ProductName,
                Code = dto.Code,
                Stock = dto.Stock,
                Price = dto.Price
            };

            var created = await _productRepo.AddAsync(product);

            var result = new ProductDto
            {
                Id = created.Id,
                ProductName = created.ProductName,
                Code = created.Code,
                Stock = created.Stock,
                Price = created.Price
            };

            return CreatedAtAction(nameof(GetById), new { id = created.Id }, result);
        }

        // PUT: /api/products/{id}
        // Editar producto: requiere autenticación
        [HttpPut("{id:int}")]
        [Authorize]
        public async Task<IActionResult> Update(int id, [FromBody] ProductDto dto)
        {
            var existing = await _productRepo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            existing.ProductName = dto.ProductName;
            existing.Code = dto.Code;
            existing.Stock = dto.Stock;
            existing.Price = dto.Price;

            await _productRepo.UpdateAsync(existing);

            return Ok(new ProductDto
            {
                Id = existing.Id,
                ProductName = existing.ProductName,
                Code = existing.Code,
                Stock = existing.Stock,
                Price = existing.Price
            });
        }

        // DELETE: /api/products/{id}
        // Solo Admin puede eliminar (cambia si quieres)
        [HttpDelete("{id:int}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var existing = await _productRepo.GetByIdAsync(id);
            if (existing == null) return NotFound();

            await _productRepo.DeleteAsync(existing);
            return NoContent();
        }
    }
}
