using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Obelisco
{
    public class Balance 
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Owner { get; set; } = null!;
        public int Coins { get; set; }
    }
}