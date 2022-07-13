# Clear Bank Code test
This is the submission for the ClearBank code test for Peter Stephenson

In this document I've explained the order of and reason for the choices I made while refactoring the PaymentService.

Note that each of these commits can be viewed in the history, if you like.

## Commit 1 - Initial
Check in initial state of test

## Commit 2 - Make the code testable
Ideally we would inject the AccountDataStore so that we can swap out the implementation in tests, I think the switching logic being done in the service would be best done when registering the service in the applications dependency container.
As there is no entrypoint and thus no DI container I have added a basic WebApi so that I can show how that code would work, this is now located in `Startup.cs` in the `ClearBank.DeveloperTest.SampleApi` project.
I noticed at this point `result.Success` was never true and set it to true prior to the validation, on the assumption this is was an oversight.
I decided to test the entrypoint using a test `WebApplicationFactory` rather than unit test the `PaymentService` directly as I have found that this approach is more robust to changes, for example when refactoring.

At this point we have a passing unit test and can proceed with the refactor.

## Commit 3 - Fully cover the remaining code
Before refactoring the code should be fully covered in tests in order to ensure that a defect is not introduced during the refactoring process.


At this point I have added tests until all code paths in the service are covered.

## Commit 4 - Move shared code

The account null check was shared across all payment schemes, so this can be moved up and only performed once.

## Commit 5 - Refactor the payment scheme rules

The last step was to refactor the payment scheme rules, I did consider pulling each out in to it's own scheme class that could encapsulate the rules for each scheme, however there's so little logic at the moment that does not need to be shared that I believe the most maintainable version of the service has all the logic located inside of it.

If significantly more rules or payment schemes were to be introduced then you may want to consider splitting each payment scheme in to separate classes to make it easier to add new ones

For example:
```cs
public class FasterPaymentsPaymentScheme : IPaymentScheme
{
  public AllowedPaymentScheme AllowedPaymentScheme => AllowedPaymentScheme.FasterPayments;

  public bool RequiresSufficientBankBalance => true;

  public bool RequiresLiveAccount => false;
}
```

Once one of these is created for each `PaymentScheme` then the code in `PaymentService` could become a lookup from the available schemes and apply generic logic