using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.IO;
using System.Text.Json.Serialization;

namespace Obelisco;

public abstract class Transaction : IEquatable<Transaction>
{
	public Transaction()
	{

	}

	public Transaction(DateTimeOffset timestamp)
	{
		Timestamp = timestamp.ToUnixTimeSeconds();
		Sender = string.Empty;
		Signature = string.Empty;
		Nonce = 0;
	}

	[Key]
	[DatabaseGenerated(DatabaseGeneratedOption.None)]
	public string Signature { get; set; }
	public long Nonce { get; set; }
	public long Timestamp { get; set; }
	public string Sender { get; set; }
	public int Index { get; set; }
	
	[JsonIgnore]
	public bool Pending { get; set; }
	

	public virtual bool Equals(Transaction other)
	{
		return Signature == other.Signature && 
			Nonce == other.Nonce &&
			Timestamp == other.Timestamp &&
			Sender == other.Sender &&
			Index == other.Index;
	}

	public override bool Equals(object obj)
	{
		return obj is Transaction other && Equals(other);
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
}