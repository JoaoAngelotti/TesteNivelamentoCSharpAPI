namespace Questao5.Domain.Entities
{
    public class Movimento
    {
        public string IdMovimento { get; set; } // Identificação única do movimento
        public string IdContaCorrente { get; set; } // Identificação única da conta corrente
        public string DataMovimento { get; set; } // Data do movimento no formato DD/MM/YYYY
        public char TipoMovimento { get; set; } // Tipo do movimento (C = Crédito, D= Débito)
        public decimal Valor { get; set; } // Valor do movimento
    }
}
