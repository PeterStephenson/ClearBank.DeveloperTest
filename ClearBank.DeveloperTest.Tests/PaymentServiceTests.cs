using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using ClearBank.DeveloperTest.Tests.Shared;
using ClearBank.DeveloperTest.Types;
using FluentAssertions;
using Moq;
using TestStack.BDDfy;
using TestStack.BDDfy.Xunit;
using Xunit;

namespace ClearBank.DeveloperTest.Tests
{
    public sealed class PaymentServiceTests : IDisposable
    {
        private readonly SystemUnderTest _sut = new SystemUnderTest();

        private HttpResponseMessage _response;

        [BddfyTheory]
        [InlineData(AllowedPaymentSchemes.Bacs, PaymentScheme.Bacs)]
        [InlineData(AllowedPaymentSchemes.Chaps, PaymentScheme.Chaps)]
        [InlineData(AllowedPaymentSchemes.FasterPayments, PaymentScheme.FasterPayments)]
        public void ValidPayment(AllowedPaymentSchemes allowedPaymentSchemes, PaymentScheme paymentScheme)
        {
            this.Given(_ => _.AnExistingCustomer("DebtorId", allowedPaymentSchemes, 20, AccountStatus.Live))
                .When(_ => _.MakingAPayment("CreditorId", "DebtorId", 10.50m, DateTime.UtcNow, paymentScheme))
                .Then(_ => _.ThePaymentIsSuccessful())
                .And(_ => _.TheNewBalanceIsStored("DebtorId", 9.5m))
                .BDDfy();
        }

        [BddfyTheory]
        [InlineData(AllowedPaymentSchemes.Chaps, PaymentScheme.Bacs)]
        [InlineData(AllowedPaymentSchemes.FasterPayments, PaymentScheme.Chaps)]
        [InlineData(AllowedPaymentSchemes.Bacs, PaymentScheme.FasterPayments)]
        public void NotAllowedPaymentScheme(AllowedPaymentSchemes allowedPaymentSchemes, PaymentScheme paymentScheme)
        {
            this.Given(_ => _.AnExistingCustomer("DebtorId", allowedPaymentSchemes, 20, AccountStatus.Live))
                .When(_ => _.MakingAPayment("CreditorId", "DebtorId", 10.50m, DateTime.UtcNow, paymentScheme))
                .Then(_ => _.ThePaymentIsNotSuccessful())
                .BDDfy();
        }

        [BddfyTheory]
        [InlineData(PaymentScheme.Bacs)]
        [InlineData(PaymentScheme.Chaps)]
        [InlineData(PaymentScheme.FasterPayments)]
        public void NoAccount(PaymentScheme paymentScheme)
        {
            this.When(_ => _.MakingAPayment("CreditorId", "DebtorId", 10.50m, DateTime.UtcNow, paymentScheme))
                .Then(_ => _.ThePaymentIsNotSuccessful())
                .BDDfy();
        }

        [BddfyFact]
        public void FasterPaymentsWithInsufficientFundsShouldFail()
        {
            this.Given(_ => _.AnExistingCustomer("DebtorId", AllowedPaymentSchemes.FasterPayments, 10, AccountStatus.Live))
                .When(_ => _.MakingAPayment("CreditorId", "DebtorId", 10.50m, DateTime.UtcNow, PaymentScheme.FasterPayments))
                .Then(_ => _.ThePaymentIsNotSuccessful())
                .BDDfy();
        }

        [BddfyTheory]
        [InlineData(AccountStatus.Disabled)]
        [InlineData(AccountStatus.InboundPaymentsOnly)]
        public void NonLiveChapsAccount(AccountStatus accountStatus)
        {
            this.Given(_ => _.AnExistingCustomer("DebtorId", AllowedPaymentSchemes.Chaps, 20, accountStatus))
                .When(_ => _.MakingAPayment("CreditorId", "DebtorId", 10.50m, DateTime.UtcNow, PaymentScheme.Chaps))
                .Then(_ => _.ThePaymentIsNotSuccessful())
                .BDDfy();
        }

        private void AnExistingCustomer(string accountNumber, AllowedPaymentSchemes paymentSchemes, decimal balance, AccountStatus status)
        {
            _sut.AccountDataStoreMock.Setup(ds => ds.GetAccount(accountNumber))
                .Returns(new Account { AccountNumber = accountNumber, AllowedPaymentSchemes = paymentSchemes, Balance = balance, Status = status });
        }

        private async Task MakingAPayment(string creditorAccountNumber, string debtorAccountNumber, decimal amount, DateTime paymentDate, PaymentScheme paymentScheme)
        {
            var body = new
            {
                creditorAccountNumber,
                debtorAccountNumber,
                amount,
                paymentDate,
                paymentScheme
            };
            _response = await _sut.Client.PostAsJsonAsync($"/customers/{debtorAccountNumber}/makePayment", body);
        }

        private void TheNewBalanceIsStored(string accountNumber, decimal newBalance)
        {
            _sut.AccountDataStoreMock.Verify(ds => ds.UpdateAccount(It.Is<Account>(a => a.AccountNumber == accountNumber && a.Balance == newBalance)));
        }

        private async Task ThePaymentIsSuccessful()
        {
            _response.StatusCode.Should().Be(HttpStatusCode.OK, await _response.Content.ReadAsStringAsync());
        }

        private async Task ThePaymentIsNotSuccessful()
        {
            _response.StatusCode.Should().Be(HttpStatusCode.BadRequest, await _response.Content.ReadAsStringAsync());
        }

        public void Dispose()
        {
            _sut?.Dispose();
            _response?.Dispose();
        }
    }
}