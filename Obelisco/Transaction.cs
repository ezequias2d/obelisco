using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;

namespace Obelisco
{
    public class Transaction
    {
        public Transaction()
        {
            Id = null!;
            Sender = null!;
            Message = null!;
        }

        public Transaction(string id, DateTime timestamp, string sender, string message, int fee = 1)
        {
            Timestamp = ((DateTimeOffset)timestamp).ToUnixTimeSeconds();
            Id = id;
            Sender = sender;
            Message = message;
            Fee = fee;
        }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Id { get; set; }
        
        public long Timestamp { get; set; }
        public string Sender { get; set; }
        public string Message { get; set; }
        public int Fee { get; set; }

        public byte[] ToBytes()
        {
            using(var stream = new MemoryStream())
            using(var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                writer.Write(Timestamp);
                writer.Write(Convert.FromBase64String(Sender));
                writer.Write(Message);
                writer.Write(Fee);

                return stream.ToArray();
            }
        }
    }
}