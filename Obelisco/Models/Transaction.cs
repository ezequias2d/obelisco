using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Obelisco;

[JsonConverterAttribute(typeof(TransactionConverter))]
public abstract class Transaction : IEquatable<Transaction>
{
    public Transaction()
    {

    }

    public Transaction(long nonce, DateTimeOffset timestamp)
    {
        Timestamp = timestamp.ToUnixTimeSeconds();
        Sender = string.Empty;
        Signature = string.Empty;
        Nonce = nonce;
    }

    public Transaction(Transaction transaction)
    {
        Signature = transaction.Signature;
        Nonce = transaction.Nonce;
        Timestamp = transaction.Timestamp;
        Sender = transaction.Sender;
        Index = transaction.Index;
        Pending = transaction.Pending;
    }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Signature { get; set; } = string.Empty;
    public long Nonce { get; set; }
    public long Timestamp { get; set; }
    public string Sender { get; set; } = string.Empty;
    public int Index { get; set; }

    [JsonIgnore]
    public bool Pending { get; set; }


    public virtual bool Equals(Transaction? other)
    {
        return other != null && Signature == other.Signature && IsSigned && other.IsSigned;
    }

    public override bool Equals(object? obj)
    {
        return obj != null && obj is Transaction other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Signature.GetHashCode();
    }

    public byte[] ToBytes()
    {
        using (var stream = new MemoryStream())
        using (var writer = new BinaryWriter(stream))
        {
            writer.Write(Nonce);
            writer.Write(Timestamp);
            writer.Write(Convert.FromBase64String(Sender));
            WriteContent(stream);

            return stream.ToArray();
        }
    }

    protected abstract void WriteContent(Stream stream);

    public void Sign(Account account)
    {
        Sender = Convert.ToBase64String(account.PublicKey);
        var data = ToBytes();
        var signature = account.SignData(data);
        Signature = Convert.ToBase64String(signature);
    }

    public bool IsSigned
    {
        get
        {
            if (Sender == null || Signature == null)
                return false;

            byte[] publicKey;
            byte[] signature;

            try
            {
                publicKey = Convert.FromBase64String(Sender);
                signature = Convert.FromBase64String(Signature);

            }
            catch (FormatException)
            {
                return false;
            }

            var account = new Account(publicKey);
            var data = ToBytes();
            return account.VerifyData(data, signature);
        }
    }

    public abstract bool Consume(Balance balance, BlockchainContext context, ILogger? logger = null);
}