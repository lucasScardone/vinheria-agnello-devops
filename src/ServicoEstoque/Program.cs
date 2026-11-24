using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServicoEstoque.Data;
using ServicoEstoque.Models;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var dbFolder = Path.Combine(AppContext.BaseDirectory, "data");
if (!Directory.Exists(dbFolder)) Directory.CreateDirectory(dbFolder);

var dbPath = Path.Combine(dbFolder, "estoque.db");
builder.Services.AddDbContext<EstoqueDbContext>(opt => opt.UseSqlite($"Data Source={dbPath}"));

// Configuração da Autenticação JWT
var key = Encoding.ASCII.GetBytes("ChaveSecretaSuperSeguraVinheriaAgnello2026!");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opt =>
    {
        opt.RequireHttpsMetadata = false;
        opt.SaveToken = true;
        opt.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Cria o banco e insere 1 produto inicial automaticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<EstoqueDbContext>();
    db.Database.EnsureCreated();
    if (!db.Produtos.Any())
    {
        db.Produtos.Add(new Produto
        {
            Nome = "Agnello Cabernet Sauvignon Reserva",
            Tipo = "Tinto",
            Uva = "Cabernet Sauvignon",
            Safra = 2021,
            Pais = "Brasil",
            Preco = 189.00m,
            Quantidade = 150
        });
        db.SaveChanges();
    }
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();