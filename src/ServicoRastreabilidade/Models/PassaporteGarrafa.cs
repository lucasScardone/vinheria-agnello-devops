namespace ServicoRastreabilidade.Models;

public class PassaporteGarrafa
{
    public int Id { get; set; }
    public string CodigoLote { get; set; } = string.Empty;
    public string Uva { get; set; } = string.Empty;
    public int Safra { get; set; }
    public string TanqueFermentacao { get; set; } = string.Empty;
    public string BarricaCarvalho { get; set; } = string.Empty;
    public bool CertificacaoEnologica { get; set; } = false;
    public double TeorAlcoolico { get; set; }
    public string MensagemAoConsumidor { get; set; } = "Da uva à taça: produzido com controle rigoroso de qualidade.";
    public DateTime UltimaAtualizacao { get; set; } = DateTime.UtcNow;
}