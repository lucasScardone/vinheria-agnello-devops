using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Client;
using ServicoQualidade.Data;
using ServicoQualidade.Models;
using System.Text.Json;

namespace ServicoQualidade.Controllers;

[ApiController]
[Route("api/[controller]")]
public class QualidadeController : ControllerBase
{
    private readonly QualidadeDbContext _db;
    private readonly IConfiguration _config;

    public QualidadeController(QualidadeDbContext db, IConfiguration config)
    {
        _db = db;
        _config = config;
    }

    [HttpGet("laudos")]
    public async Task<IActionResult> ListarLaudos() => Ok(await _db.Laudos.ToListAsync());

    [HttpGet("leituras")]
    public async Task<IActionResult> ListarLeituras() => Ok(await _db.Leituras.OrderByDescending(l => l.Id).Take(50).ToListAsync());

    [HttpPost("laudos")]
    public async Task<IActionResult> EmitirLaudo([FromBody] LaudoTecnico laudo)
    {
        _db.Laudos.Add(laudo);
        await _db.SaveChangesAsync();

        // Se aprovado, publica evento para atualizar a Rastreabilidade
        if (laudo.Aprovado)
        {
            await PublicarMqttAsync("vinheria/qualidade/lote-aprovado", laudo);
        }

        return Ok(laudo);
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
            Console.WriteLine($"[AVISO QUALIDADE MQTT] {ex.Message}");
        }
    }
}