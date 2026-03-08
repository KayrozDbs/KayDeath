using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Dalamud.Interface.Windowing;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KayDeath
{
    public sealed class Plugin : IDalamudPlugin
    {
        [PluginService] public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
        [PluginService] public static ICommandManager CommandManager { get; private set; } = null!;
        [PluginService] public static IPluginLog Log { get; private set; } = null!;
        [PluginService] public static IDataManager Data { get; private set; } = null!;
        [PluginService] public static IFramework Framework { get; private set; } = null!;
        [PluginService] public static IClientState ClientState { get; private set; } = null!;
        [PluginService] public static IObjectTable ObjectTable { get; private set; } = null!;
        [PluginService] public static IPartyList PartyList { get; private set; } = null!;
        [PluginService] public static IChatGui ChatGui { get; private set; } = null!;

        public string Name => "KayDeath";
        private const string CommandName = "/kaydeath";

        public Configuration Configuration { get; init; }
        public HistoryManager HistoryManager { get; init; }
        public DamageCollector DamageCollector { get; init; }
        public WindowSystem WindowSystem { get; init; }
        public KayDeathWindow MainWindow { get; init; }

        public Plugin()
        {
            this.Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(PluginInterface);

            this.HistoryManager = new HistoryManager();
            this.DamageCollector = new DamageCollector(this);
            this.WindowSystem = new WindowSystem("KayDeath");
            this.MainWindow = new KayDeathWindow(this);
            this.WindowSystem.AddWindow(this.MainWindow);

            Loc.Initialize(ClientState.ClientLanguage);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = Loc.T("CommandHelp")
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;

            // Subscribe to framework updates to monitor HP
            Framework.Update += OnFrameworkUpdate;
        }

        public void Dispose()
        {
            this.DamageCollector.Dispose();
            Framework.Update -= OnFrameworkUpdate;
            CommandManager.RemoveHandler(CommandName);
            this.WindowSystem.RemoveAllWindows();
        }

        private void OnCommand(string command, string args)
        {
            this.MainWindow.IsOpen = !this.MainWindow.IsOpen;
        }

        private void OnFrameworkUpdate(IFramework framework)
        {
            // Simple HP monitoring for basic implementation
            // In a real plugin, we'd also hook ActionEffect for more precision
            if (ObjectTable.LocalPlayer != null)
            {
                CheckHP(ObjectTable.LocalPlayer);
            }

            foreach (var member in PartyList)
            {
                CheckHP(member.GameObject as Dalamud.Game.ClientState.Objects.Types.ICharacter);
            }
        }

        private void CheckHP(Dalamud.Game.ClientState.Objects.Types.ICharacter? character)
        {
            if (character == null) return;
            
            if (character.CurrentHp == 0 && character.MaxHp > 0)
            {
                var rezs = GetAvailableRezs();
                var isLocal = character.GameObjectId == ObjectTable.LocalPlayer?.GameObjectId;
                var defensives = isLocal ? GetUnusedDefensives() : new List<string>();

                HistoryManager.RegisterDeath(character.GameObjectId, character.Name.TextValue, rezs, defensives);

                // Auto-open if local player died and setting is enabled
                if (isLocal && Configuration.AutoOpenOnDeath)
                {
                    MainWindow.IsOpen = true;
                }
            }
        }

        private unsafe List<string> GetAvailableRezs()
        {
            var result = new List<string>();
            foreach (var member in PartyList)
            {
                var obj = member.GameObject as Dalamud.Game.ClientState.Objects.Types.ICharacter;
                if (obj == null || obj.CurrentHp == 0) continue;

                // Simple check for MP and Job (Healer/SMN/RDM)
                if (obj.CurrentMp >= 2400)
                {
                    var jobId = obj.ClassJob.RowId;
                    bool canRez = jobId switch
                    {
                        24 or 28 or 33 or 40 => true, // Healers (WHM, SCH, AST, SGE)
                        26 or 27 => true,             // SMN (Arcanist/SMN)
                        35 => true,                   // RDM
                        _ => false
                    };

                    if (canRez)
                    {
                        result.Add($"{obj.Name} ({obj.ClassJob.Value.Abbreviation})");
                    }
                }
            }
            return result;
        }

        private unsafe List<string> GetUnusedDefensives()
        {
            var result = new List<string>();
            var actionManager = FFXIVClientStructs.FFXIV.Client.Game.ActionManager.Instance();
            if (actionManager == null || ObjectTable.LocalPlayer == null) return result;

            var player = ObjectTable.LocalPlayer;
            var playerJobId = player.ClassJob.RowId;

            // List of common defensive/mitigation IDs to check
            var defensiveIds = new uint[] 
            { 
                7507, // Rampart
                7535, // Reprisal
                7548, // Arm's Length
                7541, // Second Wind
                7536, // Addle
                7538, // Feint
                7542, // Bloodbath
                7531, // Surecast
                7537  // Swiftcast
            };

            var actionSheet = Data.GetExcelSheet<Lumina.Excel.Sheets.Action>();
            var categorySheet = Data.GetExcelSheet<Lumina.Excel.Sheets.ClassJobCategory>();

            foreach (var id in defensiveIds)
            {
                var action = actionSheet?.GetRowOrDefault(id);
                if (action == null) continue;

                // 1. Check Level
                if (player.Level < action.Value.ClassJobLevel) continue;

                // 2. Check ClassJobCategory (Can this job use this action?)
                var category = categorySheet?.GetRowOrDefault(action.Value.ClassJobCategory.RowId);
                if (category == null || !IsJobInCategory(category.Value, playerJobId)) continue;

                // 3. Check Cooldown (0 = Available)
                if (actionManager->GetRecastTime(FFXIVClientStructs.FFXIV.Client.Game.ActionType.Action, id) == 0)
                {
                    result.Add(action.Value.Name.ExtractText());
                }
            }

            return result;
        }

        private bool IsJobInCategory(Lumina.Excel.Sheets.ClassJobCategory category, uint jobId)
        {
            // The ClassJobCategory sheet has a boolean column for every job
            // Using reflection is slow, but for 10 actions on death it's fine.
            // Alternatively, we can check the abbreviation or use a hardcoded map for common ones.
            try {
                var prop = category.GetType().GetProperty(ObjectTable.LocalPlayer!.ClassJob.Value.Abbreviation.ExtractText());
                return (bool?)prop?.GetValue(category) ?? false;
            } catch { return false; }
        }

        public string GetActionName(uint actionId)
        {
            var sheet = Data.GetExcelSheet<Lumina.Excel.Sheets.Action>();
            var row = sheet?.GetRowOrDefault(actionId);
            return row?.Name.ExtractText() ?? Loc.T("Unknown");
        }

        private void DrawUI()
        {
            this.WindowSystem.Draw();
        }

        private void DrawConfigUI()
        {
            this.MainWindow.IsOpen = true;
        }
    }
}
