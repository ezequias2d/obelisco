using System.Text;
using System;
using System.Security.Cryptography;

namespace Obelisco 
{
	public class Account
	{
		public Account()
		{
			Key = ECDsa.Create(ECCurve.CreateFromFriendlyName("Curve25519"));
		}

		public Account(ReadOnlySpan<byte> publicKey)
		{
			Key = ECDsa.Create(ECCurve.CreateFromFriendlyName("Curve25519"));
			Key.ImportSubjectPublicKeyInfo(publicKey, out _);
		}

		public Account(ReadOnlySpan<byte> privateKey, ReadOnlySpan<byte> password)
		{
			Key = ECDsa.Create(ECCurve.CreateFromFriendlyName("Curve25519"));
			Key.ImportEncryptedPkcs8PrivateKey(password, privateKey, out _);
		}

		private ECDsa Key { get; }
		
		public byte[] PublicKey => Key.ExportSubjectPublicKeyInfo();
		
		public byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> password)
		{
			return Key.ExportEncryptedPkcs8PrivateKey(
				password, 
				new PbeParameters(
					PbeEncryptionAlgorithm.Aes256Cbc,
					HashAlgorithmName.SHA512,
					32
				)
			);
		}
		
		public byte[] SignData(byte[] data)
		{
			return Key.SignData(data, 0, data.Length, HashAlgorithmName.SHA512, DSASignatureFormat.Rfc3279DerSequence);
		}
		
		public bool VerifyData(byte[] data, byte[] signature)
		{
			return Key.VerifyData(data, signature, HashAlgorithmName.SHA512, DSASignatureFormat.Rfc3279DerSequence);
		}
	}
}