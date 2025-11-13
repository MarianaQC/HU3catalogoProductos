using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using catalogoProductos.Application.Interfaces;
using catalogoProductos.Application.Services;
using catalogoProductos.Infrastructure.Extensions;
using System.Text;
using Microsoft.EntityFrameworkCore;
// *** AJUSTE NECESARIO: Agrega el using a tu DbContext para la gestión de migraciones. ***
// Reemplaza 'catalogoProductos.Infrastructure.Context' con la ruta real a tu DbContext.
using catalogoProductos.Infrastructure.Context; 

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------
// CONFIGURACIÓN DE SERVICIOS
// ----------------------------------------

builder.Services.AddEndpointsApiExplorer();

// Swagger con autorización JWT (No necesita cambios internos)
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "catalogoProductos.Api", Version = "v1" });

    // Configuración de seguridad para JWT en Swagger
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Introduce el token JWT aquí (sin comillas). Ejemplo: Bearer eyJhbGciOiJIUzI1NiIs..."
    });

    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ----------------------------------------
// CONFIGURACIÓN DE JWT (CORRECCIÓN DE ROBUSTEZ)
// ----------------------------------------

var jwtSettings = builder.Configuration.GetSection("Jwt");

// *** CORRECCIÓN CRÍTICA 1: Verifica que la clave JWT exista o falla de forma controlada ***
var jwtKey = jwtSettings["Key"] ?? throw new InvalidOperationException("La clave JWT (Jwt:Key) no está configurada. Revise las Variables de Entorno en Railway.");


builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, 
            ValidateAudience = true, 
            ValidateLifetime = true, 
            ValidateIssuerSigningKey = true, 
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey) // Usa la clave verificada
            )
        };
    });

// ----------------------------------------
// AUTORIZACIÓN Y CONTROLADORES
// ----------------------------------------

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// Inyección de dependencias de la capa Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Servicio de autenticación (AuthService)
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// -------------------------------------------------------------
// ********** CORRECCIÓN CRÍTICA 2: GESTIÓN DE MIGRACIONES EN EL ARRANQUE **************
// Esto hace el arranque MÁS ROBUSTO al intentar aplicar las migraciones de la DB.
// -------------------------------------------------------------
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<catalogoProductosContext>(); // Asume el nombre de tu DbContext
    try
    {
        // Esto aplica las migraciones si existen. Si la DB no está lista, puede lanzar la excepción.
        context.Database.Migrate(); 
    }
    catch (Exception ex)
    {
        // Si falla la migración, registra el error pero permite que la aplicación intente continuar.
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error aplicando las migraciones a la Base de Datos.");
        // **IMPORTANTE:** Si tu aplicación NO PUEDE vivir sin la DB, deberías usar 'throw;'
    }
}
// *************************************************************


// ----------------------------------------
// CONFIGURACIÓN DEL PIPELINE HTTP
// ----------------------------------------

// *** CORRECCIÓN CRÍTICA 3: HABILITACIÓN DE SWAGGER EN PRODUCCIÓN ***
// Se saca del bloque IsDevelopment() para que sea visible en Railway.
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    // IMPORTANTE: Esto hace que la URL raíz (hu3catalogoproductos-production.up.railway.app/) abra Swagger
    c.RoutePrefix = string.Empty; 
});
// *************************************************************

if (app.Environment.IsDevelopment())
{
    // Se deja el bloque vacío o para otras tareas exclusivas de Dev
}

app.UseHttpsRedirection();

// Activar autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// Mapeo de controladores
app.MapControllers();

// Ejecutar la aplicación
app.Run();