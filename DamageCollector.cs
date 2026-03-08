using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Conditions;

namespace KayDeath
{
    public class DamageCollector : IDisposable
    {
        private readonly Plugin _plugin;
        private readonly Dictionary<ulong, long> _lastHp = new();
        private readonly Dictionary<ulong, uint> _activeCasts = new();

        public DamageCollector(Plugin plugin)
        {
            _plugin = plugin;
            Plugin.Framework.Update += OnUpdate;
        }

        public void Dispose()
        {
            Plugin.Framework.Update -= OnUpdate;
        }

        private void OnUpdate(IFramework framework)
        {
            if (!Plugin.ClientState.IsLoggedIn) return;
            
            // Skip processing during cutscenes (prevents errors with invalid objects)
            if (Plugin.Condition[(ConditionFlag)32]) return; // OccupiedInCutscene
            if (Plugin.Condition[(ConditionFlag)78]) return; // WatchingCutscene78

            // 1. Track NPC casts to associate actions with damage
            UpdateCastCache();

            // 2. Monitor HP changes for Local Player
            if (Plugin.ObjectTable.LocalPlayer != null)
            {
                ProcessMember(Plugin.ObjectTable.LocalPlayer);
            }

            // 3. Monitor HP changes for Party
            foreach (var member in Plugin.PartyList)
            {
                if (member != null && member.GameObject is ICharacter chara)
                {
                    ProcessMember(chara);
                }
            }
        }

        private void UpdateCastCache()
        {
            _activeCasts.Clear();
            foreach (var obj in Plugin.ObjectTable)
            {
                if (obj == null) continue;
                if (obj is IBattleChara bc && bc.IsCasting)
                {
                    // Store what this NPC is casting
                    _activeCasts[bc.GameObjectId] = bc.CastActionId;
                }
            }
        }

        private void ProcessMember(ICharacter member)
        {
            var id = member.GameObjectId;
            var currentHp = member.CurrentHp;
            
            if (_lastHp.TryGetValue(id, out var lastHp))
            {
                if (currentHp < lastHp)
                {
                    var damage = lastHp - currentHp;
                    RecordDamage(member, damage, lastHp);
                }
            }
            
            _lastHp[id] = currentHp;
        }

        private void RecordDamage(ICharacter victim, long amount, long hpBefore)
        {
            // Try to find the source
            string sourceName = Loc.T("Unknown");
            uint actionId = 0;
            string actionName = Loc.T("Unknown");
            bool foundSource = false;

            // Look for anyone targeting this victim
            foreach (var obj in Plugin.ObjectTable)
            {
                if (obj is IBattleChara bc && bc.TargetObjectId == victim.GameObjectId)
                {
                    sourceName = bc.Name.TextValue;
                    foundSource = true;
                    if (bc.IsCasting)
                    {
                        actionId = bc.CastActionId;
                        actionName = _plugin.GetActionName(actionId);
                    }
                    else
                    {
                        actionName = Loc.T("Physical");
                    }
                    break;
                }
            }

            // Environmental/Fall Detection
            if (!foundSource)
            {
                // If HP drops without a source, it's likely environmental (falls, dots, etc.)
                sourceName = Loc.T("Environment");
                actionName = victim.CurrentHp == 0 ? Loc.T("FatalFall") : Loc.T("Environmental");
            }

            var ev = new DamageEvent
            {
                Timestamp = DateTime.Now,
                SourceName = sourceName,
                ActionId = actionId,
                ActionName = actionName,
                Amount = amount,
                Type = DamageType.Unknown,
                CurrentHP = victim.CurrentHp,
                MaxHP = victim.MaxHp,
                IsKillingBlow = victim.CurrentHp == 0
            };

            _plugin.HistoryManager.AddDamage(victim.GameObjectId, ev);
        }
    }
}
