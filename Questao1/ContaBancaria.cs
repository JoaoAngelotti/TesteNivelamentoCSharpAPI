using System.Globalization;

namespace Questao1
{
    class ContaBancaria {
        public  int NumeroConta { get; } // Somente leitura
        public string NomeTitular { get; set; } //Pode ser alterado
        public double Saldo { get; private set; } // Só pode ser alterado internamente

        /// <summary>
        /// Construtor com depósito inicial opcional
        /// </summary>
        /// <param name="numeroConta">Numero da Conta do titular</param>
        /// <param name="nomeTitular">Nome do titular</param>
        /// <param name="depositoIncial">Valor do depósito inicial</param>
        public ContaBancaria(int numeroConta, string nomeTitular, double depositoIncial = 0)
        {
            NumeroConta = numeroConta;
            NomeTitular = nomeTitular;
            Saldo = depositoIncial;
        }

        /// <summary>
        /// Método para realizar depósito
        /// </summary>
        /// <param name="valor">Valor que será depositado</param>
        public void Deposito(double valor)
        {
            if (valor > 0)
            {
                Saldo += valor;
            }
        }

        /// <summary>
        /// Método para realizar saque(com taxa fixa de $ 3.50)
        /// </summary>
        /// <param name="valor">Valor que será retirado (saque)</param>
        public void Saque(double valor)
        {
            double taxa = 3.50;

            Saldo -= (valor + taxa);
        }

        /// <summary>
        /// Método para exibir os dados da conta
        /// </summary>
        /// <returns>Retorna a string com a formatação correta</returns>
        public override string ToString()
        {
            return $"Conta {NumeroConta}, Titular: {NomeTitular}, Saldo: $ {Saldo.ToString("F2", CultureInfo.InvariantCulture)}";
        }
    }
}
