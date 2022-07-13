using System;
using System.Net;
using ClearBank.DeveloperTest.Services;
using ClearBank.DeveloperTest.Types;
using Microsoft.AspNetCore.Mvc;

namespace ClearBank.DeveloperTest.SampleApi.Controllers
{
    [ApiController]
    public class MakePaymentController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public MakePaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        public class PostRequest
        {
            public string CreditorAccountNumber { get; set; }

            public decimal Amount { get; set; }

            public DateTime PaymentDate { get; set; }

            public PaymentScheme PaymentScheme { get; set; }
        }
        
        [HttpPost("/customers/{debtorAccountNumber}/makePayment")]
        public IActionResult MakePayment(string debtorAccountNumber, [FromBody]PostRequest postRequest)
        {
            var result = _paymentService.MakePayment(new MakePaymentRequest
            {
                DebtorAccountNumber = debtorAccountNumber,
                Amount = postRequest.Amount,
                PaymentDate = postRequest.PaymentDate, 
                PaymentScheme = postRequest.PaymentScheme,
                CreditorAccountNumber = postRequest.CreditorAccountNumber
            });

            if (!result.Success)
                return Problem(statusCode: (int) HttpStatusCode.BadRequest);
            
            return Ok();
        }
    }
}