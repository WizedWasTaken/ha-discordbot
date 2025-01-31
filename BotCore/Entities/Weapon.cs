using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotTemplate.BotCore.Entities
{
    public class Weapon
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int WeaponId { get; set; }

        [Required]
        public WeaponName WeaponName { get; set; }

        [Required]
        public int WeaponPrice { get; set; }

        [Required]
        public int WeaponLimit { get; set; }
    }

    public enum WeaponName
    {
        Pistol9mm,
        Vintage,
        Deagle,
        Ceramic,
        HeavyRevolver,
        Skorpion,
        Tec,
        Uzi,
        MiniAK,
        AK47,
        Gusenberg,
        PumpShotgun,
        DoubleBarrel,
        ExtendedMag,
        Suppresor,
        Ammo,
        BodyArmor,
        Scope
    }
}