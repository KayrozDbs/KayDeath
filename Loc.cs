using Dalamud.Game;
using System.Collections.Generic;

namespace KayDeath
{
    public static class Loc
    {
        private static ClientLanguage _currentLanguage = ClientLanguage.English;

        public static void Initialize(ClientLanguage language)
        {
            _currentLanguage = language;
        }

        private static readonly Dictionary<string, string> EnglishStrings = new()
        {
            { "PluginName", "KayDeath - Death Recap" },
            { "CommandHelp", "Show the KayDeath death recap." },
            { "RecentDeaths", "Recent Deaths" },
            { "NoDeaths", "No deaths recorded." },
            { "SelectRecap", "Select a death to view details." },
            { "KillingBlow", "KILLING BLOW" },
            { "Source", "Source" },
            { "Amount", "Damage" },
            { "Unknown", "Unknown" },
            { "TimeCol", "Time" },
            { "SourceCol", "Source" },
            { "ActionCol", "Action" },
            { "DamageCol", "Damage" },
            { "Physical", "Physical" },
            { "HPCol", "Remaining HP" },
            { "Environment", "Environment" },
            { "FatalFall", "Fatal Fall" },
            { "Environmental", "Environmental Damage" },
            { "SelectMember", "Select a member to see details" },
            { "CopyBtn", "Copy Recap" },
            { "SettingsTab", "Settings" },
            { "AutoOpenLabel", "Auto-open when I die" },
            { "RecapText", "[KayDeath] {0} recap: Killed by {1} ({2}) for {3:N0} damage." },
            { "OpportunityTitle", "OPPORTUNITIES & TIPS" },
            { "UnusedDefensives", "Unused Defensives" },
            { "AvailableRezs", "Available Resurrections" }
        };

        private static readonly Dictionary<string, string> FrenchStrings = new()
        {
            { "PluginName", "KayDeath - Récapitulatif de Mort" },
            { "CommandHelp", "Afficher le récapitulatif de mort KayDeath." },
            { "RecentDeaths", "Morts Récentes" },
            { "NoDeaths", "Aucune mort enregistrée." },
            { "SelectRecap", "Sélectionnez une mort pour voir les détails." },
            { "KillingBlow", "COUP FATAL" },
            { "Source", "Source" },
            { "Amount", "Dégâts" },
            { "Unknown", "Inconnu" },
            { "TimeCol", "Temps" },
            { "SourceCol", "Source" },
            { "ActionCol", "Action" },
            { "DamageCol", "Dégâts" },
            { "Physical", "Physique" },
            { "HPCol", "HP Restants" },
            { "Environment", "Environnement" },
            { "FatalFall", "Chute fatale" },
            { "Environmental", "Dégâts environnementaux" },
            { "SelectMember", "Sélectionnez un membre pour voir les détails" },
            { "CopyBtn", "Copier le récap" },
            { "SettingsTab", "Paramètres" },
            { "AutoOpenLabel", "Ouvrir automatiquement à ma mort" },
            { "RecapText", "[KayDeath] Récap de {0} : Tué par {1} ({2}) pour {3:N0} dégâts." },
            { "OpportunityTitle", "OPPORTUNITÉS & CONSEILS" },
            { "UnusedDefensives", "Défenses non utilisées" },
            { "AvailableRezs", "Resurrections disponibles" }
        };

        public static string T(string key)
        {
            var dict = _currentLanguage == ClientLanguage.French ? FrenchStrings : EnglishStrings;
            if (dict.TryGetValue(key, out var val)) return val;
            if (EnglishStrings.TryGetValue(key, out var fallback)) return fallback;
            return key;
        }

        public static string T(string key, params object[] args)
        {
            string format = T(key);
            try { return string.Format(format, args); }
            catch { return format; }
        }
    }
}
