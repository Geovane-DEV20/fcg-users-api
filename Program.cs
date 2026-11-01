using Microsoft.EntityFrameworkCore;
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

// Injeção de dependências
builder.Services.AddScoped<IUsuarioRepository, UsuarioRepository>();
builder.Services.AddScoped<CadastroService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "FCG Users API",
        Version = "v1",
        Description = "API de cadastro e gerenciamento de usuários - Microsserviço"
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

app.MapControllers();
app.Run();
