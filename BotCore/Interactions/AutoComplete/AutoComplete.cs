using BotTemplate.BotCore.Entities;
using Discord.Interactions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BotTemplate.BotCore.Interactions.AutoComplete
{
    public class AutoComplete : AutocompleteHandler
    {
        /// <summary>Generates autocomplete suggestions for the specified parameter.</summary>
        /// <param name="context"></param>
        /// <param name="autocompleteInteraction"></param>
        /// <param name="parameter"></param>
        /// <param name="services"></param>
        /// <returns>a list of autocomplete results</returns>
        public override async Task<AutocompletionResult> GenerateSuggestionsAsync(IInteractionContext context, IAutocompleteInteraction autocompleteInteraction,
            IParameterInfo parameter, IServiceProvider services)
        {
            IEnumerable<AutocompleteResult> results = Enumerable.Empty<AutocompleteResult>();
            List<string> options = new List<string>();

            // Use EventType enum values as options for the "test" parameter.
            foreach (var eventType in Enum.GetValues(typeof(EventType)))
            {
                options.Add(eventType.ToString());
            }

            // You can add more cases for each autocomplete you want to generate. Look at Slash Commands for the "test" example.
            switch (parameter.Name)
            {
                case "test":
                    results = options
                        .Select(option => new AutocompleteResult(option, option))
                        .Take(25)
                        .ToList();
                    break;

                case "eventType":
                    results = options
                        .Select(option => new AutocompleteResult(option, option))
                        .Take(25)
                        .ToList();
                    break;

                default:
                    break;
            }
            // Return the results, limited to 25 because Discord limits the number of autocomplete results.
            return AutocompletionResult.FromSuccess(results.Take(25));
        }
    }
}