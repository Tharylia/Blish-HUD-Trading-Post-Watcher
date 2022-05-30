namespace Estreya.BlishHUD.TradingPostWatcher.Models;

using Gw2Sharp.WebApi.V2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class CommerceTransaction : CommerceTransactionCurrent
{
    public enum TransactionType
    {
        Buy,
        Sell
    }

    public TransactionType Type { get; set; }

    public Item Item { get; set; }

    public bool IsHighest { get; set; }

    public CommerceTransaction(CommerceTransactionCurrent transactionCurrent)
    {
        this.Id = transactionCurrent.Id;
        this.Price = transactionCurrent.Price;
        this.Created = transactionCurrent.Created;
        this.Quantity = transactionCurrent.Quantity;
        this.ItemId = transactionCurrent.ItemId;
    }
}
