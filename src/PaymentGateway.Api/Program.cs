using FluentValidation;

using Microsoft.AspNetCore.Mvc;

using PaymentGateway.Api.Extensions;
using PaymentGateway.Api.Services;
using PaymentGateway.Api.Validation;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    // Disclosure: AI generated this block of Api Options.
    // FluentValidation owns validation; without this, [ApiController] would
    // short-circuit some requests with its own 400 before the validator runs,
    // and the same mistake would produce two different response shapes.
    options.SuppressModelStateInvalidFilter = true;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<IPaymentsRepository, PaymentsRepository>();
builder.Services.AddValidatorsFromAssemblyContaining<PostPaymentRequestValidator>();
builder.Services.AddAcquiringBank(builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();

public partial class Program { }
