using Microsoft.EntityFrameworkCore;
using ServicoRastreabilidade.Data;
using ServicoRastreabilidade.Services;

var builder = WebApplication.CreateBuilder(args);

var dbFolder = Path.Combine(AppContext.BaseDirectory, "data");
if (!Directory.Exists(dbFolder)) Directory.CreateDirectory(dbFolder);

var dbPath = Path.Combine(dbFolder, "rastreabilidade.db");
builder.Services.AddDbContext<RastreabilidadeDbContext>(opt => opt.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<MqttRastreabilidadeListener>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<RastreabilidadeDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();