using BotTemplate.BotCore.Repositories;
using Microsoft.Identity.Client;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BotTemplate.BotCore.Entities
{
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int UserId { get; set; }

        [Required]
        public ulong DiscordId { get; set; }

        [Required]
        public string IngameName { get; set; }

        [Required]
        public Role Role { get; set; }

        [Required]
        public DateTime JoinDate { get; set; } = DateTime.Now;
    }
}