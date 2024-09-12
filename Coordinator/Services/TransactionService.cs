using Coordinator.Context;
using Coordinator.Models;
using Coordinator.Services.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Coordinator.Services;

public class TransactionService(IHttpClientFactory _httpClientFactory, TwoPhaseCommitContext _context) : ITransactionService
{
    HttpClient _orderHttpClient = _httpClientFactory.CreateClient("OrderAPI");
    HttpClient _stockHttpClient = _httpClientFactory.CreateClient("StockAPI");
    HttpClient _paymentHttpClient = _httpClientFactory.CreateClient("PaymentAPI");
    public async Task<Guid> CreateTransaction()
    {
        Guid transactionId = Guid.NewGuid();

        var nodes = await _context.Nodes.ToListAsync();
        foreach (var node in nodes)
        {
            node.NodeStates = new List<NodeState>
            {
                new NodeState(transactionId)
                {
                    IsReady = Enums.ReadyType.Pending,
                    TransactionState = Enums.TransactionState.Pending
                }
            };
        }
        await _context.SaveChangesAsync();
        return transactionId;
    }

    public async Task PrepareService(Guid transactionId)
    {
        var transactionNodes = await _context.NodeStates.Include(ns => ns.Node).Where(x => x.TransactionId == transactionId).ToListAsync();

        foreach (var transactionNode in transactionNodes)
        {
            try
            {
                var response = await (transactionNode.Node.Name switch
                {
                    "Order.API" => _orderHttpClient.GetAsync("ready"),
                    "Stock.API" => _stockHttpClient.GetAsync("ready"),
                    "Payment.API" => _paymentHttpClient.GetAsync("ready"),
                });

                var result = bool.Parse(await response.Content.ReadAsStringAsync());
                transactionNode.IsReady = result ? Enums.ReadyType.Ready : Enums.ReadyType.Unready;
            }
            catch (Exception e)
            {
                transactionNode.IsReady = Enums.ReadyType.Unready;
            }
        }

        await _context.SaveChangesAsync();
    }
    public async Task<bool> CheckReadyService(Guid transactionId)
    {
        return (await _context.NodeStates.Where(x => x.TransactionId == transactionId).ToListAsync()).TrueForAll(ns => ns.IsReady == Enums.ReadyType.Ready);
    }


    public async Task Commit(Guid transactionId)
    {
        var transactionNodes = await _context.NodeStates
                                            .Where(ns => ns.TransactionId == transactionId)
                                            .Include(ns => ns.Node)
                                            .ToListAsync();

        foreach (var transactionNode in transactionNodes)
        {
            try
            {
                var response = await(transactionNode.Node.Name switch
                {
                    "Order.API" => _orderHttpClient.GetAsync("commit"),
                    "Stock.API" => _stockHttpClient.GetAsync("commit"),
                    "Payment.API" => _paymentHttpClient.GetAsync("commit")
                });

                var result = bool.Parse(await response.Content.ReadAsStringAsync());
                transactionNode.TransactionState = result ? Enums.TransactionState.Done : Enums.TransactionState.Abort;
            }
            catch
            {
                transactionNode.TransactionState = Enums.TransactionState.Abort;
            }
        }

        await _context.SaveChangesAsync();
    }
    public async Task<bool> CheckTransactionStateServices(Guid transactionId)
    {
        return (await _context.NodeStates
                    .Where(ns => ns.TransactionId == transactionId)
                    .ToListAsync())
                    .TrueForAll(ns => ns.TransactionState == Enums.TransactionState.Done);
    }
    public async Task Rollback(Guid transactionId)
    {
        var transactionNodes = await _context.NodeStates
                        .Where(ns => ns.TransactionId == transactionId)
                        .Include(ns => ns.Node)
                        .ToListAsync();

        foreach (var transactionNode in transactionNodes)
        {
            try
            {
                // İşlem gerçekleşmiş servislerde rollback işlemi yapılır.
                if (transactionNode.TransactionState == Enums.TransactionState.Done)
                    _ = await(transactionNode.Node.Name switch
                    {
                        "Order.API" => _orderHttpClient.GetAsync("rollback"),
                        "Stock.API" => _stockHttpClient.GetAsync("rollback"),
                        "Payment.API" => _paymentHttpClient.GetAsync("rollback"),
                    });

                transactionNode.TransactionState = Enums.TransactionState.Abort;
            }
            catch
            {
                transactionNode.TransactionState = Enums.TransactionState.Abort;
            }
        }

        await _context.SaveChangesAsync();
    }
}
