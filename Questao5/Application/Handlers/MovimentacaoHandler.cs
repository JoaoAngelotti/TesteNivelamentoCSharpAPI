using Dapper;
using MediatR;
using Microsoft.Data.Sqlite;
using Questao5.Application.Commands;
using Questao5.Domain.Entities;
using Questao5.Infrastructure.Sqlite;

namespace Questao5.Application.Handlers
{
    public class MovimentacaoHandler : IRequestHandler<MovimentacaoCommand, string>
    {
        private readonly DatabaseConfig _databaseConfig;

        public MovimentacaoHandler(DatabaseConfig databaseConfig)
        {
            _databaseConfig = databaseConfig;
        }

        /// <summary>
        /// Manipula o comando de movimentação financeira, realizando validações, persistindo os dados
        /// e garantindo idempotência para evitar duplicações.
        /// </summary>
        /// <param name="request">Comando contendo os dados da movimentação a ser realizada.</param>
        /// <param name="cancellationToken">Token para cancelamento da operação assíncrona.</param>
        /// <returns>Retorna o identificador da movimentação gerada.</returns>
        /// <exception cref="Exception">
        /// Lançada quando:
        /// - A conta corrente não existe.
        /// - A conta corrente está inativa.
        /// - O valor informado é inválido.
        /// - O tipo de movimentação não é reconhecido.
        /// </exception>
        public async Task<string> Handle(MovimentacaoCommand request, CancellationToken cancellationToken)
        {
            using var connection = new SqliteConnection(_databaseConfig.Name);

            // Verificar idempotência
            var idempotencia = await connection.QueryFirstOrDefaultAsync<Idempotencia>(
                "SELECT * FROM idempotencia WHERE chave_idempotencia = @ChaveIdempotencia",
                new
                {
                    ChaveIdempotencia = request.IdRequisicao
                });

            if (idempotencia != null)
            {
                return idempotencia.Resultado;
            }

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

            if (request.Valor <= 0)
            {
                var error = new ErrorResponse
                {
                    Mensagem = "O valor deve ser positivo",
                    Tipo = "INVALID_VALUE"
                };
                throw new Exception(Newtonsoft.Json.JsonConvert.SerializeObject(error));
            }

            if (request.TipoMovimento != 'C' && request.TipoMovimento != 'c' &&
                request.TipoMovimento != 'D' && request.TipoMovimento != 'd')
            {
                var error = new ErrorResponse
                {
                    Mensagem = "Tipo de movimento inválido",
                    Tipo = "INVALID_TYPE"
                };
                throw new Exception(Newtonsoft.Json.JsonConvert.SerializeObject(error));
            }

            // Inserir movimento
            var idMovimento = Guid.NewGuid().ToString();
            var dataMovimento = DateTime.Now.ToString("dd/MM/yyyy");

            await connection.ExecuteAsync(
                "INSERT INTO movimento (idmovimento, idcontacorrente, datamovimento, tipomovimento, valor) " +
                "VALUES (@IdMovimento, @IdContaCorrente, @DataMovimento, @TipoMovimento, @Valor)",
                new
                {
                    IdMovimento = idMovimento,
                    request.IdContaCorrente,
                    DataMovimento = dataMovimento,
                    request.TipoMovimento,
                    request.Valor
                });

            // Registrar idempotência
            await connection.ExecuteAsync(
                "INSERT INTO idempotencia (chave_idempotencia, requisicao, resultado) " +
                "VALUES (@ChaveIdempotencia, @Requisicao, @Resultado)",
                new
                {
                    ChaveIdempotencia = request.IdRequisicao,
                    Requisicao = Newtonsoft.Json.JsonConvert.SerializeObject(request),
                    Resultado = idMovimento
                });

            return idMovimento;
        }
    }
}
