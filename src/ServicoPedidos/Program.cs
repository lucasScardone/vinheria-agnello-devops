using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ServicoPedidos.Data;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

var dbFolder = Path.Combine(AppContext.BaseDirectory, "data");
if (!Directory.Exists(dbFolder)) Directory.CreateDirectory(dbFolder);

var dbPath = Path.Combine(dbFolder, "pedidos.db");
builder.Services.AddDbContext<PedidosDbContext>(opt => opt.UseSqlite($"Data Source={dbPath}"));

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
builder.Services.AddHttpClient();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<PedidosDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();