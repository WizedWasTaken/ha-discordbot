using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BotTemplate.BotCore.Entities
{
    public class Strike
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int StrikeId { get; set; }

        [Required]
        public string Reason { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        public int GivenToId { get; set; }

        [ForeignKey("GivenToId")]
        public User GivenTo { get; set; }

        [Required]
        public int GivenById { get; set; }

        [ForeignKey("GivenById")]
        public User GivenBy { get; set; }

        public bool IsStrikeActive()
        {
            return Date.AddMonths(1) > DateTime.Now;
        }
    }
}