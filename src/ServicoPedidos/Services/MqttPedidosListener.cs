using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Client;
using ServicoPedidos.Data;
using System.Text.Json;

namespace ServicoPedidos.Services;

public class MqttPedidosListener : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;

    public MqttPedidosListener(IServiceProvider serviceProvider, IConfiguration config)
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

            if (root.TryGetProperty("pedidoId", out var pedIdProp) && root.TryGetProperty("sucesso", out var sucProp))
            {
                var pedidoId = pedIdProp.GetInt32();
                var sucesso = sucProp.GetBoolean();

                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<PedidosDbContext>();
                var pedido = await db.Pedidos.FindAsync(pedidoId);

                if (pedido != null)
                {
                    pedido.Status = sucesso ? "Confirmado" : "Cancelado";
                    await db.SaveChangesAsync();
                    Console.WriteLine($"[SAGA] Pedido #{pedido.Id} atualizado para: {pedido.Status}");
                }
            }
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!client.IsConnected)
                {
                    await client.ConnectAsync(options, stoppingToken);
                    await client.SubscribeAsync("vinheria/estoque/resultado-reserva");
                }
            }
            catch { }
            await Task.Delay(3000, stoppingToken);
        }
    }
}