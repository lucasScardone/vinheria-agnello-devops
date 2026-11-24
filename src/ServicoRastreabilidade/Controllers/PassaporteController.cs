using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ServicoRastreabilidade.Data;

namespace ServicoRastreabilidade.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PassaporteController : ControllerBase
{
    private readonly RastreabilidadeDbContext _db;

    public PassaporteController(RastreabilidadeDbContext db) => _db = db;

    [HttpGet("{codigoLote}")]
    public async Task<IActionResult> ObterPassaporte(string codigoLote)
    {
        var passaporte = await _db.Passaportes.FirstOrDefaultAsync(p => p.CodigoLote == codigoLote);
        return passaporte is null 
            ? NotFound(new { mensagem = "Lote não localizado no sistema de rastreabilidade." }) 
            : Ok(passaporte);
    }
}