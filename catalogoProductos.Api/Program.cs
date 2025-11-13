using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using catalogoProductos.Application.Interfaces;
using catalogoProductos.Application.Services;
using catalogoProductos.Infrastructure.Extensions;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------
// CONFIGURACIÓN DE SERVICIOS
// ----------------------------------------

// Agrega servicios de Swagger (documentación API)
builder.Services.AddEndpointsApiExplorer();

// Swagger con autorización JWT
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
// CONFIGURACIÓN DE JWT
// ----------------------------------------

var jwtSettings = builder.Configuration.GetSection("Jwt");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true, // Valida el emisor
            ValidateAudience = true, // Valida el receptor
            ValidateLifetime = true, // Valida que el token no haya expirado
            ValidateIssuerSigningKey = true, // Valida la firma
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!)
            )
        };
    });

// ----------------------------------------
// AUTORIZACIÓN Y CONTROLADORES
// ----------------------------------------

builder.Services.AddAuthorization();

// Controladores de la API
builder.Services.AddControllers();

// Inyección de dependencias de la capa Infrastructure
builder.Services.AddInfrastructure(builder.Configuration);

// Servicio de autenticación (AuthService)
builder.Services.AddScoped<IAuthService, AuthService>();

var app = builder.Build();

// ----------------------------------------
// CONFIGURACIÓN DEL PIPELINE HTTP
// ----------------------------------------

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Redirección HTTPS
app.UseHttpsRedirection();

// Activar autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// Mapeo de controladores
app.MapControllers();

// Ejecutar la aplicación
app.Run();
