namespace ServicoPedidos.Models;

public class Pedido
{
    public int Id { get; set; }
    public string ClienteNome { get; set; } = string.Empty;
    public string ClienteEmail { get; set; } = string.Empty;
    public string FormaPagamento { get; set; } = string.Empty;
    public decimal ValorTotal { get; set; }
    public string Status { get; set; } = "Pendente"; // Pendente -> Confirmado ou Cancelado
    public DateTime DataCriacao { get; set; } = DateTime.UtcNow;
    public List<ItemPedido> Itens { get; set; } = new();
}