using Dapper;
using Microsoft.Data.Sqlite;
using Questao5.Application.Handlers;
using Questao5.Application.Queries;
using Questao5.Domain.Entities;
using Questao5.Infrastructure.Sqlite;
using Xunit;

namespace Questao5.Tests.Unit.Handlers
{
    public class SaldoContaCorrenteHandlerTests
    {
        private readonly DatabaseConfig _databaseConfig;
        private readonly SaldoContaCorrenteHandler _handler;

        public SaldoContaCorrenteHandlerTests()
        {
            _databaseConfig = new DatabaseConfig { Name = "Data Source=:memory:" };
            _handler = new SaldoContaCorrenteHandler(_databaseConfig);
        }

        [Fact]
        public async Task Handle_ValidAccount_ShouldReturnCorrectBalance()
        {
            // Arrange
            var query = new SaldoContaCorrenteQuerry
            {
                IdContaCorrente = "B6BAFC09-6967-ED11-A567-055DFA4A16C9"
            };

            using (var mockConnection = new SqliteConnection(_databaseConfig.Name))
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
                    INSERT INTO contacorrente(idcontacorrente, numero, nome, ativo) 
                    VALUES(@IdContaCorrente, 123, 'Titular', 1)",
                    new { IdContaCorrente = query.IdContaCorrente });
                mockConnection.Execute(@"
                    INSERT INTO movimento(idmovimento, idcontacorrente, datamovimento, tipomovimento, valor) 
                    VALUES(@IdMovimento, @IdContaCorrente, '01/01/2023', 'C', 100.50)",
                    new { IdMovimento = Guid.NewGuid().ToString(), IdContaCorrente = query.IdContaCorrente });
                mockConnection.Execute(@"
                    INSERT INTO movimento(idmovimento, idcontacorrente, datamovimento, tipomovimento, valor) 
                    VALUES(@IdMovimento, @IdContaCorrente, '02/01/2023', 'D', 50.25)",
                    new { IdMovimento = Guid.NewGuid().ToString(), IdContaCorrente = query.IdContaCorrente });
            }

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(123, result.NumeroConta);
            Assert.Equal("Titular", result.NomeTitular);
            Assert.Equal(50.25m, result.Saldo); // 100.50 (crédito) - 50.25 (débito)
        }

        [Fact]
        public async Task Handle_AccountWithNoMovements_ShouldReturnZeroBalance()
        {
            // Arrange
            var query = new SaldoContaCorrenteQuerry
            {
                IdContaCorrente = "B6BAFC09-6967-ED11-A567-055DFA4A16C9"
            };

            using (var mockConnection = new SqliteConnection(_databaseConfig.Name))
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
                    INSERT INTO contacorrente(idcontacorrente, numero, nome, ativo) 
                    VALUES(@IdContaCorrente, 123, 'Titular', 1)",
                    new { IdContaCorrente = query.IdContaCorrente });
            }

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(0m, result.Saldo);
        }

        [Fact]
        public async Task Handle_InvalidAccount_ShouldThrowInvalidAccountError()
        {
            // Arrange
            var query = new SaldoContaCorrenteQuerry
            {
                IdContaCorrente = Guid.NewGuid().ToString()
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(query, CancellationToken.None));
            var error = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorResponse>(exception.Message);

            Assert.Equal("INVALID_ACCOUNT", error.Tipo);
            Assert.Equal("Conta corrente não cadastrada", error.Mensagem);
        }

        [Fact]
        public async Task Handle_InactiveAccount_ShouldThrowInactiveAccountError()
        {
            // Arrange
            var query = new SaldoContaCorrenteQuerry
            {
                IdContaCorrente = "F475F943-7067-ED11-A06B-7E5DFA4A16C9" // Conta inativa
            };

            // Act & Assert
            var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(query, CancellationToken.None));
            var error = Newtonsoft.Json.JsonConvert.DeserializeObject<ErrorResponse>(exception.Message);

            Assert.Equal("INACTIVE_ACCOUNT", error.Tipo);
            Assert.Equal("Conta corrente inativa", error.Mensagem);
        }
    }
}
