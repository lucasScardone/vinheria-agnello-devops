using Microsoft.EntityFrameworkCore;
using ServicoProducao.Data;

var builder = WebApplication.CreateBuilder(args);

var dbFolder = Path.Combine(AppContext.BaseDirectory, "data");
if (!Directory.Exists(dbFolder)) Directory.CreateDirectory(dbFolder);

var dbPath = Path.Combine(dbFolder, "producao.db");
builder.Services.AddDbContext<ProducaoDbContext>(opt => opt.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ProducaoDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();