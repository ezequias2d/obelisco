using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using System.Threading;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Obelisco;

public class Block : IEquatable<Block>
{
    public Block()
    {

    }

    public Block(Block block)
    {
        Version = block.Version;
        Timestamp = block.Timestamp;
        Transactions = block.Transactions.Select<Transaction, Transaction>(t => t switch
        {
            PollTransaction pt => (Transaction)new PollTransaction(pt),
            TicketTransaction tt => (Transaction)new TicketTransaction(tt),
            VoteTransaction vt => (Transaction)new VoteTransaction(vt),
        }).ToList();
        Validator = block.Validator;
        Nonce = block.Nonce;
        Difficulty = block.Difficulty;
        Hash = block.Hash;
        PreviousHash = block.PreviousHash;
    }

    /// <summary>
    /// Version number, for track any changes or upgrades in protocol.
    /// </summary>
    public uint Version { get; set; }

    /// <summary>
    /// The time at which the block was hashed in unix time.
    /// </summary>
    public long Timestamp { get; set; }

    [Required]
    public virtual IList<Transaction> Transactions { get; set; } = new List<Transaction>();

    [Required]
    public string Validator { get; set; } = null!;

    public int Nonce { get; set; }

    public int Difficulty { get; set; }

    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Hash { get; set; } = null!;

    [ForeignKey(nameof(Previous))]
    public string? PreviousHash { get; set; }

    [JsonIgnore]
    public virtual Block? Previous { get; set; }

    public bool IsValid(int difficulty)
    {
        string hash = CalculateHash();
        return hash == Hash && Check(hash, difficulty);
    }

    public string CalculateHash()
    {
        var sha256 = SHA256.Create();

        var inputBytes = GetData();
        var outputBytes = sha256.ComputeHash(inputBytes);

        return Convert.ToBase64String(outputBytes);
    }

    private static bool Check(string hash, int difficulty)
    {
        int count = 0;
        for (var i = 43; i >= 0; --i)
            if (hash[i] == '0')
                count++;
        return count >= difficulty;
    }

    public bool TryMine(int difficulty, CancellationToken cancellationToken)
    {
        Hash = CalculateHash();
        while (Hash == null || !Check(Hash, difficulty))
        {
            Nonce++;
            Hash = CalculateHash();
        }
        return true;
    }

    private Stream GetData()
    {
        var stream = new MemoryStream();

        var previousHash = PreviousHash is null ? Array.Empty<byte>() : Convert.FromBase64String(PreviousHash);

        using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
        {
            writer.Write(Timestamp);
            writer.Write(previousHash);
            foreach (var transaction in Transactions)
                writer.Write(transaction.ToBytes());
            writer.Write(Nonce);
        }

        stream.Position = 0;
        return stream;
    }

    public bool Equals(Block? other)
    {
        return other != null &&
            Version == other.Version &&
            Timestamp == other.Timestamp &&
            Transactions.Count == other.Transactions.Count &&
            !Transactions.SequenceEqual(other.Transactions) &&
            Validator == other.Validator &&
            Nonce == other.Nonce &&
            Difficulty == other.Difficulty &&
            PreviousHash == other.PreviousHash &&
            Hash == other.Hash;
    }

    public override bool Equals(object? obj)
    {
        return obj is Block other && Equals(other);
    }

    public override int GetHashCode()
    {
        return Hash.GetHashCode();
    }
}
