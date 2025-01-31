using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BotTemplate.BotCore.Entities
{
    public class Event
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int EventId { get; set; }

        [Required]
        public string EventTitle { get; set; }

        public DateTime EventDate { get; set; }

        [Required]
        public string EventDescription { get; set; }

        [Required]
        public string EventLocation { get; set; }

        [Required]
        public User MadeBy { get; set; }

        [Required]
        public ICollection<User> Participants { get; set; } = new List<User>();

        [Required]
        public ICollection<User> Absent { get; set; } = new List<User>();

        [Required]
        public EventType EventType { get; set; } = EventType.Other;

        [Required]
        public string MessageID { get; set; } = "";
    }

    public enum EventType
    {
        InternalMeeting,
        ExternalMeeting,
        BandeBuy,
        Rideout,
        Other
    }
}