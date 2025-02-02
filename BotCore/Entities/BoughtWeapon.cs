using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotTemplate.BotCore.Entities
{
    public class BoughtWeapon
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int BoughtWeaponId { get; set; }

        [Required]
        public Weapon Weapon { get; set; }

        [Required]
        public User User { get; set; }

        [Required]
        public DateTime Ordered { get; set; } = DateTime.Now;

        [Required]
        public bool Paid { get; set; }

        [Required]
        public int Amount { get; set; }

        [Required]
        public bool DeliveredToUser { get; set; }
    }
}