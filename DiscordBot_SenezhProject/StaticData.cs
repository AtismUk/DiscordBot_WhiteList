using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DiscordBot_WhiteList
{
    internal static class StaticData
    {
        public static ulong moderateRole { get; set; }
        public static ulong newRole { get; set; }
        public static bool isConfigured { get; set; }
        public static string pathOfWhiteList { get; set; }
    }
}
