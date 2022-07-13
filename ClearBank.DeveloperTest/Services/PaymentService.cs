using System;
using ClearBank.DeveloperTest.Data;
using ClearBank.DeveloperTest.Types;

namespace ClearBank.DeveloperTest.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IAccountDataStore _accountDataStore;

        public PaymentService(IAccountDataStore accountDataStore)
        {
            _accountDataStore = accountDataStore;
        }

        public MakePaymentResult MakePayment(MakePaymentRequest request)
        {
            var account = _accountDataStore.GetAccount(request.DebtorAccountNumber);
            if (account == null)
            {
                return CreateFailedResult();
            }

            var allowedPaymentScheme = GetAllowedPaymentSchemeFromPaymentScheme(request.PaymentScheme);
            if (!account.AllowedPaymentSchemes.HasFlag(allowedPaymentScheme))
            {
                return CreateFailedResult();
            }
            
            if(!AccountCanMakePayment(account, request))
            {
                return CreateFailedResult();
            }

            account.Balance -= request.Amount;
            _accountDataStore.UpdateAccount(account);

            return new MakePaymentResult { Success = true };
        }

        private static bool AccountCanMakePayment(Account account, MakePaymentRequest request) =>
            request.PaymentScheme switch
            {
                PaymentScheme.FasterPayments => account.Balance > request.Amount,
                PaymentScheme.Chaps => account.Status == AccountStatus.Live,
                _ => true
            };

        private static AllowedPaymentSchemes GetAllowedPaymentSchemeFromPaymentScheme(PaymentScheme scheme) =>
            scheme switch
            {
                PaymentScheme.FasterPayments => AllowedPaymentSchemes.FasterPayments,
                PaymentScheme.Bacs => AllowedPaymentSchemes.Bacs,
                PaymentScheme.Chaps => AllowedPaymentSchemes.Chaps,
                _ => throw new ArgumentOutOfRangeException(nameof(scheme), scheme, $"AllowedPaymentScheme not found for PaymentScheme {scheme.ToString()}")
            };

        private static MakePaymentResult CreateFailedResult() => new MakePaymentResult { Success = false };
    }
}