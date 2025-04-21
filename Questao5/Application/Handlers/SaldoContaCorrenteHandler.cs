using Dapper;
using MediatR;
using Microsoft.Data.Sqlite;
using Questao5.Application.Queries;
using Questao5.Domain.Entities;
using Questao5.Infrastructure.Sqlite;

namespace Questao5.Application.Handlers
{
    public class SaldoContaCorrenteHandler : IRequestHandler<SaldoContaCorrenteQuerry, SaldoContaCorrente>
    {
        private readonly DatabaseConfig _databaseConfig;

        public SaldoContaCorrenteHandler(DatabaseConfig databaseConfig)
        {
            _databaseConfig = databaseConfig;
        }

        /// <summary>
        /// Manipula a consulta de saldo de uma conta corrente, realizando validações e o cálculo do saldo com base nos movimentos registrados.
        /// </summary>
        /// <param name="request">Consulta contendo o identificador da conta corrente.</param>
        /// <param name="cancellationToken">Token para cancelamento da operação assíncrona.</param>
        /// <returns>Retorna os dados da conta corrente com o saldo atual calculado.</returns>
        /// <exception cref="Exception">
        /// Lançada quando:
        /// - A conta corrente não existe.
        /// - A conta corrente está inativa.
        /// </exception>
        public async Task<SaldoContaCorrente> Handle(SaldoContaCorrenteQuerry request, CancellationToken cancellationToken)
        {
            using var connection = new SqliteConnection(_databaseConfig.Name);

            // Validações
            var conta = await connection.QueryFirstOrDefaultAsync<ContaCorrente>(
                "SELECT * FROM contacorrente WHERE idcontacorrente = @IdContaCorrente",
                new
                {
                    request.IdContaCorrente
                });

            if (conta == null)
            {
                var error = new ErrorResponse
                {
                    Mensagem = "Conta corrente não cadastrada",
                    Tipo = "INVALID_ACCOUNT"
                };
                throw new Exception(Newtonsoft.Json.JsonConvert.SerializeObject(error));
            }

            if (!conta.Ativo)
            {
                var error = new ErrorResponse
                {
                    Mensagem = "Conta corrente inativa",
                    Tipo = "INACTIVE_ACCOUNT"
                };
                throw new Exception(Newtonsoft.Json.JsonConvert.SerializeObject(error));
            }

            // Calcular saldo
            var movimentos = await connection.QueryAsync<Movimento>(
                "SELECT * FROM movimento WHERE idcontacorrente = @IdContaCorrente",
                new
                {
                    request.IdContaCorrente
                });

            decimal saldo = 0;

            foreach (var movimento in movimentos)
            {
                if (movimento.TipoMovimento == 'C' || movimento.TipoMovimento == 'c')
                {
                    saldo += movimento.Valor;
                }
                else
                {
                    saldo -= movimento.Valor;
                }
            }

            return new SaldoContaCorrente
            {
                NumeroConta = conta.Numero,
                NomeTitular = conta.Nome,
                DataHoraConsulta = DateTime.Now,
                Saldo = saldo
            };
        }
    }
}
