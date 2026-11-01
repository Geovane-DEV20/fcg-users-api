using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using fcg_users_api.Data;
using fcg_users_api.Repositories;
using fcg_users_api.Services;
using fcg_users_api.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Configuração do DbContext
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    // Para desenvolvimento, usando InMemory. Em produção, usar SQL Server ou PostgreSQL
    options.UseInMemoryDatabase("FcgUsersDb");
    
    // Para SQL Server, descomentar:
    // options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"));
});

// Configuração JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JWT SecretKey não configurada");
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // Em produção, usar true
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidateAudience = true,
        ValidAudience = jwtSettings["Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => policy.RequireRole("Administrador"));
    options.AddPolicy("UserOrAdmin", policy => policy.RequireRole("Usuario", "Administrador"));
});

// Injeção de dependências
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<CadastroService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<TokenService>();

// Configuração de mensageria (RabbitMQ)
builder.Services.AddSingleton<fcg_users_api.Events.IEventPublisher, fcg_users_api.Events.RabbitMQ.RabbitMQEventPublisher>();

// Health Checks
builder.Services.AddHealthChecks()
    .AddCheck<fcg_users_api.Events.RabbitMQ.RabbitMQHealthCheck>("rabbitmq");

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "FCG Users API",
        Version = "v1",
        Description = "API de cadastro e gerenciamento de usuários - Microsserviço com Autenticação JWT"
    });

    // Configuração para JWT no Swagger
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Description = "JWT Authorization header usando o esquema Bearer. \r\n\r\n" +
                      "Digite 'Bearer' [espaço] e então seu token no campo abaixo.\r\n\r\n" +
                      "Exemplo: 'Bearer 12345abcdef'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
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

var app = builder.Build();

// Middleware de tratamento de exceções
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

// Redirecionar raiz para Swagger
app.MapGet("/", () => Results.Redirect("/swagger")).ExcludeFromDescription();

// Health Check endpoint
app.MapHealthChecks("/health");

app.MapControllers();
app.Run();
