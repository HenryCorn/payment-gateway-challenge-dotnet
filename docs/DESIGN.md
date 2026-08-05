# Design decisions
Decisions and assumptions for the assignment.

## Validation

- Expiry in the future: if it's (currentMonth/currentYear) I've set the solution to mark it as expired. This is how I understood the briefing was asking it.

- Injecting the TimeProvider into the PostPaymentRequestValidator: Allows me to be able to test the rule for expiration in the future.

- Amount must be greater than zero: this is not specified in the briefing, so I've assumed that IRL sending a payment of 0 to a bank is not something we'd like to do.

## Models

- Payment created to be the domain model separate from request/response DTOs. It's the only that holds the sensitive information. ToString overriden to avoid leaking information in the logs.

- Request properties nullable so the validator can properly report errors in the request (I believe otherwise it would bind to 0 in some cases instead of null). In that case we would be getting "must be between 1 and 12" for the expiry month instead of a message complaining about the month being required.

- CVVs and card numbers are strings to preserve leading zeros.
