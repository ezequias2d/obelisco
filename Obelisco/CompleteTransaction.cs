using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace Obelisco
{
    [Table("CompleteTransaction")]
    public class CompleteTransaction : Transaction
    {
        public CompleteTransaction()
        {
        }

        public CompleteTransaction(string id, DateTime timestamp, string sender, string message, int fee = 1) 
            : base(id, timestamp, sender, message, fee)
        {
        }

        public CompleteTransaction(Transaction transaction) 
        {
            Timestamp = transaction.Timestamp;
            Sender = transaction.Sender;
            Message = transaction.Message;
            Fee = transaction.Fee;
        }
    }
}