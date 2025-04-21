using MediatR;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Questao5.Application.Commands;
using Questao5.Application.Queries;
using Questao5.Controllers;
using Questao5.Domain.Entities;
using Xunit;

namespace Questao5.Tests.Unit.Controllers
{
    public class ContaCorrenteControllerTests
    {
        private readonly IMediator _mediator;
        private readonly ContaCorrenteController _controller;

        public ContaCorrenteControllerTests()
        {
            _mediator = Substitute.For<IMediator>();
            _controller = new ContaCorrenteController(_mediator);
        }

        [Fact]
        public async Task Movimentacao_ValidCommand_ShouldReturnOk()
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
            _mediator.Send(command).Returns(expectedId);

            // Act
            var result = await _controller.Movimentacao(command);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedId, okResult.Value);
        }

        [Fact]
        public async Task Movimentacao_InvalidCommand_ShouldReturnBadRequest()
        {
            // Arrange
            var command = new MovimentacaoCommand
            {
                IdRequisicao = Guid.NewGuid().ToString(),
                IdContaCorrente = Guid.NewGuid().ToString(), // Conta inválida
                Valor = 100,
                TipoMovimento = 'C'
            };

            var error = new ErrorResponse { Tipo = "INVALID_ACCOUNT", Mensagem = "Conta inválida" };
            _mediator.Send(command).Returns<Task>(x => throw new Exception(Newtonsoft.Json.JsonConvert.SerializeObject(error)));

            // Act
            var result = await _controller.Movimentacao(command);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var returnedError = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("INVALID_ACCOUNT", returnedError.Tipo);
        }

        [Fact]
        public async Task Saldo_ValidAccount_ShouldReturnOk()
        {
            // Arrange
            var idConta = "B6BAFC09-6967-ED11-A567-055DFA4A16C9";
            var expectedSaldo = new SaldoContaCorrente
            {
                NumeroConta = 123,
                NomeTitular = "Titular",
                DataHoraConsulta = DateTime.Now,
                Saldo = 100.50m
            };

            _mediator.Send(Arg.Any<SaldoContaCorrenteQuerry>()).Returns(expectedSaldo);

            // Act
            var result = await _controller.Saldo(idConta);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(expectedSaldo, okResult.Value);
        }

        [Fact]
        public async Task Saldo_InvalidAccount_ShouldReturnBadRequest()
        {
            // Arrange
            var idConta = Guid.NewGuid().ToString();
            var error = new ErrorResponse { Tipo = "INVALID_ACCOUNT", Mensagem = "Conta inválida" };
            _mediator.Send(Arg.Any<SaldoContaCorrenteQuerry>())
                .Returns<Task<SaldoContaCorrente>>(x => throw new Exception(Newtonsoft.Json.JsonConvert.SerializeObject(error)));

            // Act
            var result = await _controller.Saldo(idConta);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            var returnedError = Assert.IsType<ErrorResponse>(badRequestResult.Value);
            Assert.Equal("INVALID_ACCOUNT", returnedError.Tipo);
        }
    }
}
