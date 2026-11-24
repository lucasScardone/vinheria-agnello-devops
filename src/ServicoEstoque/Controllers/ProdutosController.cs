using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServicoEstoque.Data;
using ServicoEstoque.Models;

namespace ServicoEstoque.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Exige Token JWT válido em todas as rotas deste Controller
public class ProdutosController : ControllerBase
{
    private readonly EstoqueDbContext _db;

    public ProdutosController(EstoqueDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> Listar()
    {
        return Ok(await _db.Produtos.ToListAsync());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> ObterPorId(int id)
    {
        var produto = await _db.Produtos.FindAsync(id);
        return produto is null ? NotFound("Produto não encontrado.") : Ok(produto);
    }

    [HttpPost]
    public async Task<IActionResult> Criar([FromBody] Produto produto)
    {
        _db.Produtos.Add(produto);
        await _db.SaveChangesAsync();
        return CreatedAtAction(nameof(ObterPorId), new { id = produto.Id }, produto);
    }

    [HttpPost("reservar")]
    public async Task<IActionResult> Reservar([FromBody] ReservaDto dto)
    {
        var produto = await _db.Produtos.FindAsync(dto.ProdutoId);
        if (produto is null)
            return NotFound(new { mensagem = "Produto não encontrado no estoque." });

        if (produto.Quantidade < dto.Quantidade)
            return BadRequest(new { mensagem = "Saldo insuficiente no estoque." });

        produto.Quantidade -= dto.Quantidade;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            mensagem = "Estoque reservado com sucesso via comunicação segura JWT!",
            produto = produto.Nome,
            saldoRestante = produto.Quantidade
        });
    }
}

public class ReservaDto
{
    public int ProdutoId { get; set; }
    public int Quantidade { get; set; }
}