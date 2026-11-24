namespace ServicoProducao.Models;

public class LoteProducao
{
    public int Id { get; set; }
    public string CodigoLote { get; set; } = string.Empty; // Ex: LOTE-2026-01
    public string Uva { get; set; } = string.Empty;        // Cabernet Sauvignon
    public int Safra { get; set; }                         // 2026
    public string TanqueFermentacao { get; set; } = string.Empty; // Tanque-T01
    public string BarricaCarvalho { get; set; } = string.Empty;   // Carvalho Francês B-12
    public DateTime DataInicio { get; set; } = DateTime.UtcNow;
}