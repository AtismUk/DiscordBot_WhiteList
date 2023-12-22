using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot_WhiteList.Models
{
    internal class CreatorEmbedModel
    {
        public string Titel { get; set; }
        public Color Color { get; set; }
        public Dictionary<string, string> Fields { get; set; }
        public EmbedAuthorBuilder Author { get; set; } = null!;

        public CreatorEmbedModel(string titel, Dictionary<string, string> fields, Color color, EmbedAuthorBuilder authorBuilder = null!)
        {
            Fields = fields;
            Color = color;
            Titel = titel;
            Author = authorBuilder;
        }
    }
}
