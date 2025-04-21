namespace Questao5.Domain.Entities
{
    public class SaldoContaCorrente
    {
        public int NumeroConta { get; set; } // Numero da conta
        public string NomeTitular { get; set; } // Nome do titular
        public DateTime DataHoraConsulta { get; set; } // Data atual
        public decimal Saldo { get; set; } // Saldo da conta
    }
}
