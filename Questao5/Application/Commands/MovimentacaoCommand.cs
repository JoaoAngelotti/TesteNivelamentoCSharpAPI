using MediatR;
using Questao5.Domain.Entities;

namespace Questao5.Application.Commands
{
    public class MovimentacaoCommand : IRequest<string>
    {
        public string IdRequisicao { get; set; } // Identificação da requisição
        public string IdContaCorrente { get; set; } // Identificação da conta corrente
        public decimal Valor { get; set; } // Valor que será movimentado
        public char TipoMovimento { get; set; } // Tipo do movimento (C = Crédito, D = Débito)
    }
}
