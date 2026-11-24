using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Client;
using ServicoProducao.Data;
using ServicoProducao.Models;
using System.Text.Json;

namespace ServicoProducao.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LotesController : ControllerBase
{
    private readonly ProducaoDbContext _db;
    private readonly IConfiguration _config;

    public LotesController(ProducaoDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> Listar() => Ok(await _db.Lotes.ToListAsync());

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] LoteProducao lote)
    {
        _db.Lotes.Add(lote);
        await _db.SaveChangesAsync();

        // Notifica o barramento MQTT que um novo lote foi iniciado
        await PublicarMqttAsync("vinheria/producao/lote-criado", lote);

        return CreatedAtAction(nameof(Listar), new { id = lote.Id }, lote);
    }

    private async Task PublicarMqttAsync(string topico, object payload)
    {
        try
        {
            var factory = new MqttFactory();
            using var client = factory.CreateMqttClient();
            var host = _config["MQTT_HOST"] ?? "broker-mqtt";
            var options = new MqttClientOptionsBuilder().WithTcpServer(host, 1883).Build();

            await client.ConnectAsync(options);
            var msg = new MqttApplicationMessageBuilder()
                .WithTopic(topico)
                .WithPayload(JsonSerializer.Serialize(payload))
                .Build();
            await client.PublishAsync(msg);
            await client.DisconnectAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[AVISO PRODUCAO MQTT] {ex.Message}");
        }
    }
}