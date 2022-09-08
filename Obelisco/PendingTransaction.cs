using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Obelisco
{
    [Table("PendingTransaction")]
    public class PendingTransaction : Transaction
    {
        public PendingTransaction()
        {
        }

        public PendingTransaction(string id, DateTime timestamp, string sender, string message, int fee = 1) 
            : base(id, timestamp, sender, message, fee)
        {
        }

        public PendingTransaction(Transaction transaction) 
        {
            Timestamp = transaction.Timestamp;
            Sender = transaction.Sender;
            Message = transaction.Message;
            Fee = transaction.Fee;
        }
    }
}