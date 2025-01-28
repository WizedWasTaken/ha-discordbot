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
        public User GivenTo { get; set; }

        [Required]
        public User GivenBy { get; set; }

        public bool IsStrikeActive()
        {
            return Date.AddMonths(1) > DateTime.Now;
        }
    }
}