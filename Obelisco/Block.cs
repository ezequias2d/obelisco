using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Obelisco
{
    public class Block
    {
        public Block()
        {

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
        public virtual IList<CompleteTransaction> Transactions { get; set; } = null!;

        [Required]
        public string Validator { get; set; } = null!;

        public int Nonce { get; set; }

        public int Difficulty { get; set; }

        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Hash { get; set; } = null!;

        [ForeignKey("Previous")]
        public string? PreviousHash { get; set; }

        public virtual Block? Previous { get; set; }

        [InverseProperty("Previous")]
        public virtual Block? Next { get; set; }

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
            for(var i = 43; i >= 0; --i)
                if (hash[i] == '0')
                    count++;
            return count >= difficulty;
        }

        public void Mine(int difficulty)
        {
            while (Hash == null || !Check(Hash, difficulty))
            {
                Nonce++;
                Hash = CalculateHash();
            }
        }

        private Stream GetData() 
        {
            var stream = new MemoryStream();

            var previousHash = PreviousHash is null ? Array.Empty<byte>() : Convert.FromBase64String(PreviousHash);

            using(var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
            {
                writer.Write(Timestamp);
                writer.Write(previousHash);
                foreach(var transaction in Transactions)
                    writer.Write(transaction.ToBytes());
                writer.Write(Nonce);
            }
            
            stream.Position = 0;
            return stream;
        }
    }
}
