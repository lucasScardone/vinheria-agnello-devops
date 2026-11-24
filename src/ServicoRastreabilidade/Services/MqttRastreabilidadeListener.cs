using Microsoft.EntityFrameworkCore;
using MQTTnet;
using MQTTnet.Client;
using ServicoRastreabilidade.Data;
using ServicoRastreabilidade.Models;
using System.Text.Json;

namespace ServicoRastreabilidade.Services;

public class MqttRastreabilidadeListener : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _config;

    public MqttRastreabilidadeListener(IServiceProvider serviceProvider, IConfiguration config)
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
            try
            {
                var topico = e.ApplicationMessage.Topic;
                var payload = System.Text.Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);

                using var doc = JsonDocument.Parse(payload);
                var root = doc.RootElement;

                using var scope = _serviceProvider.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<RastreabilidadeDbContext>();

                if (topico == "vinheria/producao/lote-criado")
                {
                    var codigo = ObterTexto(root, "codigoLote");
                    var passaporte = await db.Passaportes.FirstOrDefaultAsync(p => p.CodigoLote == codigo);

                    if (passaporte == null)
                    {
                        passaporte = new PassaporteGarrafa
                        {
                            CodigoLote = codigo,
                            Uva = ObterTexto(root, "uva"),
                            Safra = ObterInteiro(root, "safra"),
                            TanqueFermentacao = ObterTexto(root, "tanqueFermentacao"),
                            BarricaCarvalho = ObterTexto(root, "barricaCarvalho"),
                            UltimaAtualizacao = DateTime.UtcNow
                        };
                        db.Passaportes.Add(passaporte);
                        await db.SaveChangesAsync();
                        Console.WriteLine($"[RASTREABILIDADE SUCESSO] Passaporte criado para: {codigo}");
                    }
                }
                else if (topico == "vinheria/qualidade/lote-aprovado")
                {
                    var codigo = ObterTexto(root, "codigoLote");
                    var passaporte = await db.Passaportes.FirstOrDefaultAsync(p => p.CodigoLote == codigo);

                    if (passaporte != null)
                    {
                        passaporte.CertificacaoEnologica = true;
                        passaporte.TeorAlcoolico = ObterDouble(root, "teorAlcoolico");
                        passaporte.UltimaAtualizacao = DateTime.UtcNow;
                        await db.SaveChangesAsync();
                        Console.WriteLine($"[RASTREABILIDADE SUCESSO] Lote {codigo} certificado e aprovado!");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO RASTREABILIDADE] {ex.Message}");
            }
        };

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!client.IsConnected)
                {
                    await client.ConnectAsync(options, stoppingToken);
                    await client.SubscribeAsync("vinheria/producao/lote-criado");
                    await client.SubscribeAsync("vinheria/qualidade/lote-aprovado");
                    Console.WriteLine("[RASTREABILIDADE] Conectado e ouvindo tópicos de Produção e Qualidade.");
                }
            }
            catch { }
            await Task.Delay(3000, stoppingToken);
        }
    }

    // Métodos auxiliares para ler propriedades sem se importar com maiúsculas/minúsculas
    private static string ObterTexto(JsonElement el, string nome)
    {
        foreach (var p in el.EnumerateObject())
            if (string.Equals(p.Name, nome, StringComparison.OrdinalIgnoreCase))
                return p.Value.GetString() ?? "";
        return "";
    }

    private static int ObterInteiro(JsonElement el, string nome)
    {
        foreach (var p in el.EnumerateObject())
            if (string.Equals(p.Name, nome, StringComparison.OrdinalIgnoreCase))
                return p.Value.GetInt32();
        return 0;
    }

    private static double ObterDouble(JsonElement el, string nome)
    {
        foreach (var p in el.EnumerateObject())
            if (string.Equals(p.Name, nome, StringComparison.OrdinalIgnoreCase))
                return p.Value.GetDouble();
        return 0;
    }
}