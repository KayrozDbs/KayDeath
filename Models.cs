using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;

namespace KayDeath
{
    public enum DamageType : byte
    {
        Unknown = 0,
        Slashing = 1,
        Piercing = 2,
        Blunt = 3,
        Shot = 4,
        Magic = 5,
        Healing = 6,
        LimitBreak = 7,
        Darkness = 8
    }

    public class DamageEvent
    {
        public DateTime Timestamp { get; set; }
        public uint ActionId { get; set; }
        public string SourceName { get; set; } = string.Empty;
        public string ActionName { get; set; } = string.Empty;
        public long Amount { get; set; }
        public DamageType Type { get; set; }
        public uint CurrentHP { get; set; }
        public uint MaxHP { get; set; }
        public bool IsCritical { get; set; }
        public bool IsDirectHit { get; set; }
        public bool IsKillingBlow { get; set; }

        public float HPPercent => MaxHP > 0 ? (float)CurrentHP / MaxHP : 0;
    }

    public class DeathRecap
    {
        public string PlayerName { get; set; } = string.Empty;
        public DateTime DeathTime { get; set; }
        public List<DamageEvent> Events { get; set; } = new();
        public List<string> AvailableRezs { get; set; } = new();
        public List<string> UnusedDefensives { get; set; } = new();
        public DamageEvent? KillingBlow => Events.Find(e => e.IsKillingBlow);
    }
}
