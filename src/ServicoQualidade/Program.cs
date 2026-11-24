using Microsoft.EntityFrameworkCore;
using ServicoQualidade.Data;
using ServicoQualidade.Services;

var builder = WebApplication.CreateBuilder(args);

var dbFolder = Path.Combine(AppContext.BaseDirectory, "data");
if (!Directory.Exists(dbFolder)) Directory.CreateDirectory(dbFolder);

var dbPath = Path.Combine(dbFolder, "qualidade.db");
builder.Services.AddDbContext<QualidadeDbContext>(opt => opt.UseSqlite($"Data Source={dbPath}"));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<MqttSensorListener>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<QualidadeDbContext>();
    db.Database.EnsureCreated();
}

app.UseSwagger();
app.UseSwaggerUI();
app.MapControllers();
app.Run();