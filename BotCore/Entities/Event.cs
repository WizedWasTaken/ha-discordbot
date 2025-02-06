using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

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

        public ICollection<User> Participants { get; set; } = new List<User>();

        public ICollection<User> Absent { get; set; } = new List<User>();

        [Required]
        public EventType EventType { get; set; } = EventType.Other;

        [Required]
        public string MessageID { get; set; } = "";

        [Required]
        public EventStatus EventStatus { get; set; } = EventStatus.Open;

        public List<BoughtWeapon> WeaponsBought { get; set; } = new List<BoughtWeapon>();

        public bool CanAddWeaponToEvent()
        {
            if (EventType != EventType.BandeBuy)
            {
                return false;
            }

            return true;
        }
    }

    public enum EventStatus
    {
        Open,
        Closed
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