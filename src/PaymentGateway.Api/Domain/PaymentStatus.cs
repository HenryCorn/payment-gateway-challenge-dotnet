using System.Text.Json.Serialization;

namespace PaymentGateway.Api.Domain;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentStatus
{
    Authorized,
    Declined,
    Rejected
}