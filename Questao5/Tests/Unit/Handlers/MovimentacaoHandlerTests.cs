using Dapper;
using Microsoft.Data.Sqlite;
using NSubstitute;
using Questao5.Application.Commands;
using Questao5.Application.Handlers;
using Questao5.Domain.Entities;
using Questao5.Infrastructure.Sqlite;
using System.Data;
using Xunit;

namespace Questao5.Tests.Unit.Handlers
{
    public class MovimentacaoHandlerTests
    {
        private readonly DatabaseConfig _databaseConfig;
        private readonly IDbConnection _dbConnection;
        private readonly MovimentacaoHandler _handler;

        public MovimentacaoHandlerTests()
        {
            _databaseConfig = Substitute.For<DatabaseConfig>();
            _dbConnection = Substitute.For<IDbConnection>();
            _handler = new MovimentacaoHandler(_databaseConfig);

            // Configuração mock para o connection factory
            _databaseConfig.Name.Returns("Data Source=:memory:");
        }

        [Fact]
        public async Task Handle_ValidCreditCommand_ShouldReturnMovementId()
        {
            // Arrange
            var command = new MovimentacaoCommand
            {
                IdRequisicao = Guid.NewGuid().ToString(),
                IdContaCorrente = "B6BAFC09-6967-ED11-A567-055DFA4A16C9",
                Valor = 100.50m,
                TipoMovimento = 'C'
            };

            var contaAtiva = new ContaCorrente
            {
                IdContaCorrente = command.IdContaCorrente,
                Numero = 123,
                Nome = "Titular",
                Ativo = true
            };

            using (var mockConnection = new SqliteConnection("Data Source=:memory:"))
            {
                mockConnection.Open();
                mockConnection.Execute(@"
                CREATE TABLE contacorrente (
                    idcontacorrente TEXT(37) PRIMARY KEY,
                    numero INTEGER(10) NOT NULL UNIQUE,
                    nome TEXT(100) NOT NULL,
                    ativo INTEGER(1) NOT NULL default 0,
                    CHECK(ativo in (0, 1))
            ");
                mockConnection.Execute(@"
                CREATE TABLE movimento (
                    idmovimento TEXT(37) PRIMARY KEY,
                    idcontacorrente TEXT(37) NOT NULL,
                    datamovimento TEXT(25) NOT NULL,
                    tipomovimento TEXT(1) NOT NULL,
                    valor REAL NOT NULL,
                    CHECK(tipomovimento in ('C', 'D')),
                    FOREIGN KEY(idcontacorrente) REFERENCES contacorrente(idcontacorrente))
            ");
                mockConnection.Execute(@"
                CREATE TABLE idempotencia (
                    chave_idempotencia TEXT(37) PRIMARY KEY,
                    requisicao TEXT(1000),
                    resultado TEXT(1000))
            ");
                mockConnection.Execute(@"
                INSERT INTO contacorrente(idcontacorrente, numero, nome, ativo) 
                VALUES(@IdContaCorrente, @Numero, @Nome, @Ativo)",
                    contaAtiva);
            }

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }

        [Fact]
        public async Task Handle_InvalidAccount_ShouldThrowInvalidAccountError()
        {
            // Arrange
            var command = new MovimentacaoCommand
            {
                IdRequisicao = Guid.NewGuid().ToString(),
                IdContaCorrente = Guid.NewGuid().ToString(),
                Valor = 100,
                TipoMovimento = 'C'
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
            var error = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorResponse>(exception.Message);

            Assert.Equal("INVALID_ACCOUNT", error.Tipo);
            Assert.Equal("Conta corrente não cadastrada", error.Mensagem);
        }

        [Fact]
        public async Task Handle_InactiveAccount_ShouldThrowInactiveAccountError()
        {
            // Arrange
            var command = new MovimentacaoCommand
            {
                IdRequisicao = Guid.NewGuid().ToString(),
                IdContaCorrente = "F475F943-7067-ED11-A06B-7E5DFA4A16C9", // Conta inativa
                Valor = 100,
                TipoMovimento = 'C'
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
            var error = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorResponse>(exception.Message);

            Assert.Equal("INACTIVE_ACCOUNT", error.Tipo);
            Assert.Equal("Conta corrente inativa", error.Mensagem);
        }

        [Fact]
        public async Task Handle_InvalidValue_ShouldThrowInvalidValueError()
        {
            // Arrange
            var command = new MovimentacaoCommand
            {
                IdRequisicao = Guid.NewGuid().ToString(),
                IdContaCorrente = "B6BAFC09-6967-ED11-A567-055DFA4A16C9",
                Valor = -10,
                TipoMovimento = 'C'
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
            var error = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorResponse>(exception.Message);

            Assert.Equal("INVALID_VALUE", error.Tipo);
            Assert.Equal("Valor deve ser positivo", error.Mensagem);
        }

        [Fact]
        public async Task Handle_InvalidType_ShouldThrowInvalidTypeError()
        {
            // Arrange
            var command = new MovimentacaoCommand
            {
                IdRequisicao = Guid.NewGuid().ToString(),
                IdContaCorrente = "B6BAFC09-6967-ED11-A567-055DFA4A16C9",
                Valor = 100,
                TipoMovimento = 'X' // Tipo inválido
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(command, CancellationToken.None));
            var error = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorResponse>(exception.Message);

            Assert.Equal("INVALID_TYPE", error.Tipo);
            Assert.Equal("Tipo de movimento inválido", error.Mensagem);
        }

        [Fact]
        public async Task Handle_DuplicateRequest_ShouldReturnSameMovementId()
        {
            // Arrange
            var command = new MovimentacaoCommand
            {
                IdRequisicao = Guid.NewGuid().ToString(),
                IdContaCorrente = "B6BAFC09-6967-ED11-A567-055DFA4A16C9",
                Valor = 100,
                TipoMovimento = 'C'
            };

            var expectedId = Guid.NewGuid().ToString();

            using (var mockConnection = new SqliteConnection("Data Source=:memory:"))
            {
                mockConnection.Open();
                mockConnection.Execute(@"
                INSERT INTO idempotencia (chave_idempotencia, requisicao, resultado) 
                VALUES (@ChaveIdempotencia, @Requisicao, @Resultado)",
                    new
                    {
                        ChaveIdempotencia = command.IdRequisicao,
                        Requisicao = Newtonsoft.Json.JsonConvert.SerializeObject(command),
                        Resultado = expectedId
                    });
            }

            // Act
            var result = await _handler.Handle(command, CancellationToken.None);

            // Assert
            Assert.Equal(expectedId, result);
        }
    }
}
