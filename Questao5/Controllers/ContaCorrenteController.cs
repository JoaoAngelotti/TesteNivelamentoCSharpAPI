using MediatR;
using Microsoft.AspNetCore.Mvc;
using Questao5.Application.Commands;
using Questao5.Application.Queries;
using Questao5.Domain.Entities;

namespace Questao5.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ContaCorrenteController : ControllerBase
    {
        private readonly IMediator _mediator;

        public ContaCorrenteController(IMediator mediator)
        {
            _mediator = mediator;
        }

        /// <summary>
        /// Realiza uma movimentação financeira com base nos dados enviados no corpo da requisição.
        /// </summary>
        /// <param name="command">Comando contendo os dados da movimentação a ser realizada.</param>
        /// <returns>Retorna o identificador da movimentação realizada ou um erro em caso de falha.</returns>
        [HttpPost("movimentacao")]
        [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Movimentacao([FromBody] MovimentacaoCommand command)
        {
            try
            {
                var idMovimento = await _mediator.Send(command);
                return Ok(idMovimento);
            }
            catch (Exception ex)
            {
                var error = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorResponse>(ex.Message);
                return BadRequest(error);
            }
        }

        /// <summary>
        /// Obtém o saldo atual da conta corrente especificada.
        /// </summary>
        /// <param name="idContaCorrente">Identificador da conta corrente.</param>
        /// <returns>Retorna o saldo da conta ou um erro em caso de falha.</returns>
        [HttpGet("saldo/{idContaCorrente}")]
        [ProducesResponseType(typeof(SaldoContaCorrente), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Saldo(string idContaCorrente)
        {
            try
            {
                var query = new SaldoContaCorrenteQuerry
                {
                    IdContaCorrente = idContaCorrente
                };
                var saldo = await _mediator.Send(query);
                return Ok(saldo);
            }
            catch (Exception ex)
            {
                var error = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorResponse>(ex.Message);
                return BadRequest(error);
            }
        }
    }
}
