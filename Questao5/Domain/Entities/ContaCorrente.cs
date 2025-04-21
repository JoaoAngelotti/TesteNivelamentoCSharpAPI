namespace Questao5.Domain.Entities
{
    public class ContaCorrente
    {
        public string IdContaCorrente { get; set; } // ID da conta corrente
        public int Numero { get; set; } // Numero da conta corrente
        public string Nome { get; set; } // Nome do titular da conta corrente
        public bool Ativo { get; set; } // Indicativo se a conta está ativa
    }
}
