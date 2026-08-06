# Design decisions
Decisions and assumptions for the assignment.

## Validation

- Expiry in the future: if it's (currentMonth/currentYear) I've set the solution to mark it as expired. This is how I understood the briefing was asking it.

- Injecting the TimeProvider into the PostPaymentRequestValidator: Allows me to be able to test the rule for expiration in the future.

- Amount must be greater than zero: this is not specified in the briefing, so I've assumed that IRL sending a payment of 0 to a bank is not something we'd like to do.

## Models

- Payment is the domain model, kept separate from the request/response DTOs. It overrides `ToString`, because as a record it would print every property. In a class like `PostPaymentRequest` it returns just the type name, so nothing to redact. Payment is a validated PostPaymentRequest.

- Request properties nullable so the validator can properly report errors in the request (I believe otherwise it would bind to 0 in some cases instead of null). In that case we would be getting "must be between 1 and 12" for the expiry month instead of a message complaining about the month being required.

- CVVs and card numbers are strings to preserve leading zeros.

## Acquiring Bank

- Added retries just for when the bank didn't process the request. Not retrying timeouts to be on the safe side at avoiding charging 2 times for the same transaction. (only a declined is an actual declined).

- HttpClient via IHttpClientFactory as it was easy to inject the resilience policy.

- With the validation in place it should be impossible to get InvalidRequest from the bank, so if that happens it's a bug and logging it as a error

- Not parsing the error message from the bank because it uses multiple names for the same property, sticking to the contract instead.

- Added integration test project to check how my implemntation of the client would behave with a mock of the acquiring bank.


## Payments Endpoint

- Only call the bank if validation passes, and return informative error messages when it's invalid

- Declined comes as 201 because it's a created payment but resolved to be Declined.

- Suppressed the automatic [ApiController] model-state filter so validation has exactly one path and one response shape.

- Unavailable maps to 502 rather than 503. 503 would say this gateway is down, and it isn't, the bank is.

- InvalidRequest maps to 500. My own validation should make it impossible, so if it fires it's a bug in my request mapping.

- The orchestration (validate, call the bank, map the outcome, store, respond) lives in the controller rather than a service class.
For two endpoints and one flow, a PaymentsService would have exactly one caller. The interfaces I inject already give me the seams I need for testing.

## Storage

- In-memory as per the brief, swapped the List for a ConcurrentDictionary because the repository is a singleton and requests can be concurrent.

- Storing response shape to avoid keeping sensitive data.

- Sticked with the GUIDs for payment requests.

## Known gaps

- No idempotency key on POST /api/Payments. Every call generates a new GUID and authorizes with the bank, so a merchant retrying after a timeout would create a second payment and a second authorization. Acquiring bank didn't have it and I was trying to avoid over-engineering.
