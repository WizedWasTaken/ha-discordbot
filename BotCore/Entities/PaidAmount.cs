using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BotTemplate.BotCore.Entities
{
    public class PaidAmount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int PaidAmountId { get; set; }

        [Required]
        public decimal Amount { get; set; }

        [Required]
        public List<User> PaidTo { get; set; } = new List<User>();

        [Required]
        public User PaidBy { get; set; }

        [Required]
        public Event BandeBuyEvent { get; set; }
    }
}