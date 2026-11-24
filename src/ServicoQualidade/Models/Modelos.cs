namespace ServicoQualidade.Models;

public class LeituraSensor
{
    public int Id { get; set; }
    public string Tipo { get; set; } = string.Empty; // Temperatura, Umidade, Luminosidade
    public double Valor { get; set; }
    public DateTime DataHora { get; set; } = DateTime.UtcNow;
}

public class LaudoTecnico
{
    public int Id { get; set; }
    public string CodigoLote { get; set; } = string.Empty;
    public double Ph { get; set; }
    public double TeorAlcoolico { get; set; }
    public bool Aprovado { get; set; }
    public string Observacoes { get; set; } = string.Empty;
    public DateTime DataEmissao { get; set; } = DateTime.UtcNow;
}