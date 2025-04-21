using MediatR;
using Questao5.Domain.Entities;

namespace Questao5.Application.Queries
{
    public class SaldoContaCorrenteQuerry : IRequest<SaldoContaCorrente>
    {
        public string IdContaCorrente { get; set; } // Identificação da conta corrente
    }
}
