using MQTTnet;
using MQTTnet.Client;
using ServicoQualidade.Data;
using ServicoQualidade.Models;
using System.Text.Json;

namespace ServicoQualidade.Services;

public class MqttSensorListener : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;

    public MqttSensorListener(IServiceProvider serviceProvider, IConfiguration config)
    {
        _serviceProvider = serviceProvider;
        _config = config;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var factory = new MqttFactory();
        var client = factory.CreateMqttClient();
        var host = _config["MQTT_HOST"] ?? "broker-mqtt";

        var options = new MqttClientOptionsBuilder().WithTcpServer(host, 1883).Build();

        client.ApplicationMessageReceivedAsync += async e =>
        {
            var topico = e.ApplicationMessage.Topic;
            var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<QualidadeDbContext>();

            try
            {
                using var doc = JsonDocument.Parse(payload);
                double valor = 0;
                string tipo = "Geral";

                if (topico.Contains("temperatura") && doc.RootElement.TryGetProperty("temperatura", out var t))
                {
                    valor = t.GetDouble();
                    tipo = "Temperatura";
                }
                else if (topico.Contains("umidade") && doc.RootElement.TryGetProperty("umidade", out var u))
                {
                    valor = u.GetDouble();
                    tipo = "Umidade";
                }

                db.Leituras.Add(new LeituraSensor { Tipo = tipo, Valor = valor });
                await db.SaveChangesAsync();
                Console.WriteLine($"[IoT RECEBIDO] {tipo}: {valor}");
            }
            catch { }
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!client.IsConnected)
                {
                    await client.ConnectAsync(options, stoppingToken);
                    await client.SubscribeAsync("fiap/iot/3esoa/GrupoEngSoftware/#");
                    await client.SubscribeAsync("vinheria/iot/#");
                }
            }
            catch { }
            await Task.Delay(3000, stoppingToken);
        }
    }
}