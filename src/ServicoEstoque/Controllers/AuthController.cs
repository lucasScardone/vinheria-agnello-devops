using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ServicoEstoque.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private const string ChaveSecreta = "ChaveSecretaSuperSeguraVinheriaAgnello2026!";

    [HttpPost("login")]
    public IActionResult Login([FromBody] LoginRequest req)
    {
        if (req.Usuario != "admin" || req.Senha != "agnello123")
            return Unauthorized(new { mensagem = "Usuário ou senha inválidos." });

        var key = Encoding.ASCII.GetBytes(ChaveSecreta);
        var tokenConfig = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, req.Usuario),
                new Claim(ClaimTypes.Role, "Microsservico")
            }),
            Expires = DateTime.UtcNow.AddHours(2),
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenConfig);

        return Ok(new
        {
            usuario = req.Usuario,
            token = tokenHandler.WriteToken(token)
        });
    }
}

public class LoginRequest
{
    public string Usuario { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
}