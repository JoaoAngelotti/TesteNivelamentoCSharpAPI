namespace Questao5.Domain.Entities
{
    public class Idempotencia
    {
        public string ChaveIdempotencia { get; set; } // Identificação da chave de idempotencia
        public string Requisicao { get; set; } // Dados de requisição
        public string Resultado { get; set; } // Dados de retorno
    }
}
