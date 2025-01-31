using Discord.Interactions;

namespace BotTemplate.BotCore.Interactions.Modals
{
    public class Modals : InteractionsCore
    {
        /// <summary>Represents a modal for echoing user input.</summary>
        public class EchoConfirmModal : IModal
        {
            public string Title => "Confirmation";

            [InputLabel("What to Echo?")]
            [ModalTextInput("echo", TextInputStyle.Paragraph, placeholder: "Enter text to echo")]
            public string? EchoText { get; set; }
        }

        /// <summary>Handles the submission of the echo_confirm Modal. Extracts the text then responds</summary>
        [ModalInteraction("echo_confirm")]
        public async Task OnTestModalSubmit(EchoConfirmModal modal)
        {
            // Extract the submitted text from the modal
            string submittedText = modal.EchoText ?? "No text was submitted.";
            await RespondAsync($"You said: {submittedText}", ephemeral: true);
        }

        public class OpretEventModal : IModal
        {
            public string Title => "Opret Event";

            [InputLabel("Event Navn")]
            [ModalTextInput("event_name", TextInputStyle.Paragraph, placeholder: "Indtast event navn")]
            public string? EventName { get; set; }

            [InputLabel("Beskrivelse")]
            [ModalTextInput("event_description", TextInputStyle.Paragraph, placeholder: "Indtast event beskrivelse")]
            public string? EventDescription { get; set; }

            [InputLabel("Dato")]
            [ModalTextInput("event_date", TextInputStyle.Paragraph, placeholder: "Indtast event dato")]
            public string? EventDate { get; set; }

            [InputLabel("Lokation")]
            [ModalTextInput("event_location", TextInputStyle.Paragraph, placeholder: "Indtast event lokation")]
            public string? EventLocation { get; set; }
        }

        [ModalInteraction("opret_event")]
        public async Task<List<string>> OnEventModalSubmit(OpretEventModal modal)
        {
            List<string> strings = new List<string>();
            string eventName = modal.EventName ?? "No event name was submitted.";
            string eventDescription = modal.EventDescription ?? "No event description was submitted.";
            string eventDate = modal.EventDate ?? "No event date was submitted.";
            string eventLocation = modal.EventLocation ?? "No event location was submitted.";

            strings.Add(eventName);
            strings.Add(eventDescription);
            strings.Add(eventDate);
            strings.Add(eventLocation);

            return strings;
        }
    }
}