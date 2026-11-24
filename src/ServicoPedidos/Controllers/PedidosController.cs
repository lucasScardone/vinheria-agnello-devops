using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServicoPedidos.Data;
using ServicoPedidos.Models;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace ServicoPedidos.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Protegido por Token JWT
public class PedidosController : ControllerBase
{
    private readonly PedidosDbContext _db;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;

    public PedidosController(PedidosDbContext db, IHttpClientFactory httpClientFactory, IConfiguration config)
    {
        _db = db;
        _httpClientFactory = httpClientFactory;
        _config = config;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        return Ok(await _db.Pedidos.Include(p => p.Itens).ToListAsync());
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] Pedido pedido)
    {
        var client = _httpClientFactory.CreateClient();

        // Pega o Token JWT enviado na requisição e repassa para o Serviço de Estoque
        var authHeader = Request.Headers["Authorization"].ToString();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            var token = authHeader.Substring("Bearer ".Length).Trim();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        // Resolve o endereço do Estoque via DNS interno do Docker
        var estoqueUrl = _config["ESTOQUE_URL"] ?? "http://servico-estoque:5005";

        foreach (var item in pedido.Itens)
        {
            var payload = JsonSerializer.Serialize(new
            {
                produtoId = item.ProdutoId,
                quantidade = item.Quantidade
            });

            var content = new StringContent(payload, Encoding.UTF8, "application/json");
            var response = await client.PostAsync($"{estoqueUrl}/api/produtos/reservar", content);

            if (!response.IsSuccessStatusCode)
            {
                var erro = await response.Content.ReadAsStringAsync();
                return BadRequest(new { mensagem = "Falha ao reservar no Serviço de Estoque.", detalhe = erro });
            }
        }

        pedido.ValorTotal = pedido.Itens.Sum(i => i.Quantidade * i.PrecoUnitario);
        pedido.Status = "Confirmado (Reserva Segura via JWT)";
        pedido.DataCriacao = DateTime.UtcNow;

        _db.Pedidos.Add(pedido);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(Listar), new { id = pedido.Id }, pedido);
    }
}