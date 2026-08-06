# Payment Gateway

An API that lets a merchant process a card payment through an acquiring bank and
retrieve a previously made payment. It validates the request itself, calls the
bank only when the request is valid, stores the result and reports it back.

The assignment this was built against is in [`docs/CHALLENGE_BRIEF.md`](docs/CHALLENGE_BRIEF.md).

## Quickstart

```bash
docker compose up -d                    # bank simulator + Aspire dashboard
dotnet run --project src/PaymentGateway.Api
```

The API listens on `https://localhost:7092` and `http://localhost:5067`, and
opens Swagger at `https://localhost:7092/swagger`. The telemetry dashboard is at
`http://localhost:18888`.

```bash
dotnet test                             # no Docker needed for this one
```

## Test cards

The bank simulator keys off the last digit of the card number.

| Card number | Last digit | What happens |
|---|---|---|
| `2222405343248877` | odd | Authorized |
| `2222405343248874` | even | Declined |
| `2222405343248870` | zero | Bank returns 503 — the gateway retries, then gives up |

Ready-to-run requests for all of these are in [`http/payments.http`](http/payments.http)
and [`http/validation.http`](http/validation.http).

## The API

### `POST /api/Payments`

```json
{
  "cardNumber": "2222405343248877",
  "expiryMonth": 4,
  "expiryYear": 2030,
  "currency": "GBP",
  "amount": 100,
  "cvv": "123"
}
```

```json
{
  "id": "0c797210-0032-4b88-b8a0-b7705fc1c002",
  "status": "Authorized",
  "cardNumberLastFour": "8877",
  "expiryMonth": 4,
  "expiryYear": 2030,
  "currency": "GBP",
  "amount": 100
}
```

`amount` is an integer in the currency's minor unit — `100` is £1.00. `currency`
must be one of `GBP`, `USD`, `EUR`. The response never contains the full card
number or the CVV.

### `GET /api/Payments/{id}`

Returns the same body as the POST that created it, or `404` if the id is unknown.

### Status codes

| Situation | Status | Why |
|---|---|---|
| Bank authorized | `201 Created` | A payment was created, with `Location` pointing at it |
| Bank declined | `201 Created` | A payment was still created — `Declined` is a real outcome, not a failure of the request |
| Request failed validation | `422 Unprocessable Entity` | The body parsed fine, the values were wrong. Rejected without calling the bank |
| Body could not be parsed | `400 Bad Request` | Malformed JSON or a wrongly typed field, so there is nothing to validate |
| Bank unavailable | `502 Bad Gateway` | An upstream dependency is down, not this gateway — `503` would claim the wrong thing |
| Payment id unknown | `404 Not Found` | |

The two that surprise people: **Declined is a 201**, because the merchant asked
us to create a payment and we did — its status happens to be `Declined`. And
invalid input is **422, not 400**, because 400 is reserved here for a body the
API could not read at all.

## Project structure

```
src/PaymentGateway.Api/
    Contracts/Merchant/       request and response shapes the merchant sees
    Contracts/AcquiringBank/  request and response shapes the bank sees
    Domain/                   Payment, the validated instruction, and its outcomes
    Services/                 bank client, repository, metrics
    Validation/               FluentValidation rules
test/
    PaymentGateway.Api.Tests             unit tests
    PaymentGateway.Api.IntegrationTests  anything that needs a socket
    PaymentGateway.Api.TestUtils         shared request builder
```

`Contracts` is split by whose contract it is: changing what the bank sends us
should never mean editing a type the merchant also depends on. The test split
follows one rule — if a test opens a port, it belongs in the integration
project. `dotnet test` runs both, and neither needs Docker.

## More

- [`docs/DESIGN.md`](docs/DESIGN.md) — the decisions and assumptions behind all of the above
- [`docs/DevOps.md`](docs/DevOps.md) — CI and the automated PR review
- [`docs/NOTES.md`](docs/NOTES.md) — working notes kept while building it
- [`docs/CHALLENGE_BRIEF.md`](docs/CHALLENGE_BRIEF.md) — the original assignment
