using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Client;
using ServicoEstoque.Data;
using System.Text.Json;

namespace ServicoEstoque.Services;

public class MqttEstoqueListener : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;

    public MqttEstoqueListener(IServiceProvider serviceProvider, IConfiguration config)
    {
        _serviceProvider = serviceProvider;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        var client = factory.CreateMqttClient();

        var host = _config["MQTT_HOST"] ?? "broker-mqtt";
        var port = int.TryParse(_config["MQTT_PORT"], out var p) ? p : 1883;

        var options = new MqttClientOptionsBuilder().WithTcpServer(host, port).Build();

        client.ApplicationMessageReceivedAsync += async e =>
        {
            var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            using var doc = JsonDocument.Parse(payload);
            var root = doc.RootElement;

            if (root.TryGetProperty("pedidoId", out var pedIdProp) && root.TryGetProperty("itens", out var itensProp))
            {
                var pedidoId = pedIdProp.GetInt32();
                bool reservaComSucesso = true;

                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<EstoqueDbContext>();

                // Verifica e reserva cada item
                foreach (var item in itensProp.EnumerateArray())
                {
                    var prodId = item.GetProperty("produtoId").GetInt32();
                    var qtd = item.GetProperty("quantidade").GetInt32();

                    var prod = await db.Produtos.FindAsync(prodId);
                    if (prod == null || prod.Quantidade < qtd)
                    {
                        reservaComSucesso = false;
                        break;
                    }
                    prod.Quantidade -= qtd; // Reserva física do lote
                }

                if (reservaComSucesso)
                {
                    await db.SaveChangesAsync();
                    Console.WriteLine($"[SAGA] Estoque reservado para Pedido #{pedidoId}.");
                }

                // Responde para a Saga
                var resposta = new { pedidoId = pedidoId, sucesso = reservaComSucesso };
                var respMsg = new MqttApplicationMessageBuilder()
                    .WithTopic("vinheria/estoque/resultado-reserva")
                    .WithPayload(JsonSerializer.Serialize(resposta))
                    .Build();

                await client.PublishAsync(respMsg);
            }
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!client.IsConnected)
                {
                    await client.ConnectAsync(options, stoppingToken);
                    await client.SubscribeAsync("vinheria/pedidos/criado");
                }
            }
            catch { }
            await Task.Delay(3000, stoppingToken);
        }
    }
}