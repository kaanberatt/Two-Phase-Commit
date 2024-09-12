using Coordinator.Context;
using Coordinator.Services.Abstractions;
using Coordinator.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddDbContext<TwoPhaseCommitContext>(x => 
    x.UseSqlServer(builder.Configuration.GetConnectionString("SqlConnection"))
    );

var orderApiUrl = builder.Configuration["ApiSettings:OrderAPI"];
var stockApiUrl = builder.Configuration["ApiSettings:StockAPI"];
var paymentApiUrl = builder.Configuration["ApiSettings:PaymentAPI"];

builder.Services.AddHttpClient("OrderAPI", client =>
{
    client.BaseAddress = new Uri(orderApiUrl);
});

builder.Services.AddHttpClient("StockAPI", client =>
{
    client.BaseAddress = new Uri(stockApiUrl);
});

builder.Services.AddHttpClient("PaymentAPI", client =>
{
    client.BaseAddress = new Uri(paymentApiUrl);
});
builder.Services.AddTransient<ITransactionService, TransactionService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.MapGet("/create-order-transaction", async (ITransactionService transactionService) =>
{
    //Phase 1 - Prepare
    var transactionId = await transactionService.CreateTransaction();
    await transactionService.PrepareService(transactionId);
    bool transactionState = await transactionService.CheckReadyService(transactionId);

    if (transactionState)
    {
        //Phase 2 - Commit
        await transactionService.Commit(transactionId);
        transactionState = await transactionService.CheckTransactionStateServices(transactionId);
    }

    if (!transactionState)
        await transactionService.Rollback(transactionId);
});
app.Run();
