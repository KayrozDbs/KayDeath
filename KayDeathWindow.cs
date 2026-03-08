using Dalamud.Interface.Windowing;
using ImGui = Dalamud.Bindings.ImGui.ImGui;
using ImGuiCond = Dalamud.Bindings.ImGui.ImGuiCond;
using ImGuiCol = Dalamud.Bindings.ImGui.ImGuiCol;
using ImGuiStyleVar = Dalamud.Bindings.ImGui.ImGuiStyleVar;
using ImGuiTableFlags = Dalamud.Bindings.ImGui.ImGuiTableFlags;
using ImGuiTableColumnFlags = Dalamud.Bindings.ImGui.ImGuiTableColumnFlags;
using ImGuiTableBgTarget = Dalamud.Bindings.ImGui.ImGuiTableBgTarget;
using ImGuiWindowFlags = Dalamud.Bindings.ImGui.ImGuiWindowFlags;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace KayDeath
{
    public class KayDeathWindow : Window
    {
        private readonly Plugin _plugin;
        private DeathRecap? _selectedRecap;

        public KayDeathWindow(Plugin plugin) : base(Loc.T("PluginName"))
        {
            _plugin = plugin;
            Size = new Vector2(800, 500);
            SizeCondition = ImGuiCond.FirstUseEver;
        }

        public override void Draw()
        {
            PushKayTheme();

            if (ImGui.BeginTabBar("KayDeathTabs"))
            {
                if (ImGui.BeginTabItem(Loc.T("RecentDeaths")))
                {
                    DrawMainTab();
                    ImGui.EndTabItem();
                }

                if (ImGui.BeginTabItem(Loc.T("SettingsTab")))
                {
                    DrawSettingsTab();
                    ImGui.EndTabItem();
                }
                ImGui.EndTabBar();
            }

            PopKayTheme();
        }

        private void DrawMainTab()
        {
            if (ImGui.BeginTable("MainLayout", 2, ImGuiTableFlags.Resizable))
            {
                ImGui.TableSetupColumn("List", ImGuiTableColumnFlags.WidthFixed, 200);
                ImGui.TableSetupColumn("Detail", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableNextRow();

                // Left Column: Recent Deaths
                ImGui.TableSetColumnIndex(0);
                DrawDeathList();

                // Right Column: Recap Detail
                ImGui.TableSetColumnIndex(1);
                DrawRecapDetail();

                ImGui.EndTable();
            }
        }

        private void DrawSettingsTab()
        {
            ImGui.BeginChild("SettingsChild");
            
            var autoOpen = _plugin.Configuration.AutoOpenOnDeath;
            if (ImGui.Checkbox(Loc.T("AutoOpenLabel"), ref autoOpen))
            {
                _plugin.Configuration.AutoOpenOnDeath = autoOpen;
                _plugin.Configuration.Save();
            }

            ImGui.EndChild();
        }

        private void DrawDeathList()
        {
            ImGui.TextColored(new Vector4(0.01f, 0.95f, 1f, 1f), Loc.T("RecentDeaths"));
            ImGui.Separator();
            ImGui.Spacing();

            var deaths = _plugin.HistoryManager.GetAllDeaths();
            if (!deaths.Any())
            {
                ImGui.TextDisabled(Loc.T("NoDeaths"));
                return;
            }

            foreach (var recap in deaths)
            {
                bool isSelected = _selectedRecap == recap;
                if (ImGui.Selectable($"{recap.PlayerName}##{recap.DeathTime.Ticks}", isSelected))
                {
                    _selectedRecap = recap;
                }
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - 40);
                ImGui.TextDisabled(recap.DeathTime.ToString("HH:mm"));
            }
        }

        private void DrawRecapDetail()
        {
            if (_selectedRecap == null)
            {
                ImGui.Text(Loc.T("SelectMember"));
                return;
            }

            // 1. Killing Blow Card (Fixed at top)
            DrawKillingBlowCard();

            // 2. Scrollable Content (Timeline + Opportunities)
            // Reserve space for the button at the bottom
            var footerHeight = ImGui.GetFrameHeightWithSpacing() + 10;
            if (ImGui.BeginChild("RecapScrollArea", new Vector2(0, -footerHeight), false, ImGuiWindowFlags.AlwaysVerticalScrollbar))
            {
                DrawTimelineTable();
                DrawOpportunitiesSection();
                ImGui.EndChild();
            }

            // 3. Copy Button (Fixed at bottom)
            ImGui.Separator();
            if (ImGui.Button($"{Loc.T("CopyBtn")}##Recap", new Vector2(-1, 0)))
            {
                CopyRecapToClipboard();
            }
        }

        private void DrawKillingBlowCard()
        {
            if (_selectedRecap == null) return;
            var kb = _selectedRecap.KillingBlow;
            if (kb == null) return;

            // Header with Killing Blow info - Modern card look
            ImGui.PushStyleColor(ImGuiCol.ChildBg, ImGui.ColorConvertFloat4ToU32(new Vector4(0.12f, 0.12f, 0.15f, 1.0f)));
            if (ImGui.BeginChild("RecapHeader", new Vector2(0, 90), true))
            {
                ImGui.Columns(2, "HeaderCols", false);
                ImGui.SetColumnWidth(0, ImGui.GetWindowWidth() * 0.6f);

                ImGui.TextDisabled(Loc.T("KillingBlow").ToUpper());
                ImGui.SetWindowFontScale(1.4f);
                ImGui.TextColored(new Vector4(0.01f, 0.95f, 1f, 1f), kb.ActionName);
                ImGui.SetWindowFontScale(1.0f);

                ImGui.NextColumn();

                ImGui.BeginGroup();
                ImGui.TextDisabled(Loc.T("SourceCol"));
                ImGui.TextColored(new Vector4(1f, 0.9f, 0.4f, 1f), kb.SourceName);
                ImGui.EndGroup();

                ImGui.SameLine(ImGui.GetColumnWidth() - 80);
                ImGui.BeginGroup();
                ImGui.TextDisabled(Loc.T("DamageCol"));
                ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), $"{kb.Amount:N0}");
                ImGui.EndGroup();

                ImGui.Columns(1);
                ImGui.EndChild();
            }
            ImGui.PopStyleColor();

            ImGui.Spacing();
        }

        private void DrawTimelineTable()
        {
            if (ImGui.BeginTable("Timeline", 5, ImGuiTableFlags.Borders | ImGuiTableFlags.RowBg))
            {
                ImGui.TableSetupColumn(Loc.T("TimeCol"), ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn(Loc.T("SourceCol"), ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn(Loc.T("ActionCol"), ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(Loc.T("DamageCol"), ImGuiTableColumnFlags.WidthFixed, 80);
                ImGui.TableSetupColumn(Loc.T("HPCol"), ImGuiTableColumnFlags.WidthFixed, 110); // Reduced width to avoid cut-off
                ImGui.TableHeadersRow();

                if (_selectedRecap != null)
                {
                    foreach (var ev in _selectedRecap.Events.AsEnumerable().Reverse())
                    {
                    ImGui.TableNextRow();
                    if (ev.IsKillingBlow)
                        ImGui.TableSetBgColor(ImGuiTableBgTarget.RowBg0, ImGui.GetColorU32(new Vector4(0.6f, 0.15f, 0.15f, 0.4f)));

                    ImGui.TableSetColumnIndex(0);
                    ImGui.TextDisabled($"{(ev.Timestamp - _selectedRecap.DeathTime).TotalSeconds:F1}s");
                    ImGui.TableSetColumnIndex(1);
                    ImGui.Text(ev.SourceName);
                    ImGui.TableSetColumnIndex(2);
                    ImGui.Text(ev.ActionName);
                    ImGui.TableSetColumnIndex(3);
                    ImGui.TextColored(GetDamageColor(ev.Type), ev.Amount.ToString());
                    ImGui.TableSetColumnIndex(4);
                    DrawHPBar(ev.HPPercent, ev.CurrentHP, ev.MaxHP);
                    }
                }
                ImGui.EndTable();
            }
        }

        private void DrawOpportunitiesSection()
        {
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.TextColored(new Vector4(1f, 0.8f, 0.2f, 1f), Loc.T("OpportunityTitle"));
            
            if (ImGui.BeginTable("Opportunities", 2, ImGuiTableFlags.None))
            {
                ImGui.TableNextRow();
                ImGui.TableSetColumnIndex(0);
                ImGui.TextDisabled(Loc.T("UnusedDefensives"));
                if (_selectedRecap != null && _selectedRecap.UnusedDefensives.Any())
                {
                    foreach (var def in _selectedRecap.UnusedDefensives)
                        ImGui.TextColored(new Vector4(0.4f, 1f, 0.4f, 1f), $"• {def}");
                }
                else ImGui.Text("-");

                ImGui.TableSetColumnIndex(1);
                ImGui.TextDisabled(Loc.T("AvailableRezs"));
                if (_selectedRecap != null && _selectedRecap.AvailableRezs.Any())
                {
                    foreach (var rez in _selectedRecap.AvailableRezs)
                        ImGui.TextColored(new Vector4(0.4f, 0.8f, 1f, 1f), $"• {rez}");
                }
                else ImGui.Text("-");

                ImGui.EndTable();
            }
        }

        private void DrawHPBar(float percent, uint current, uint max)
        {
            var color = percent > 0.5f ? new Vector4(0.2f, 0.8f, 0.2f, 1f) : (percent > 0.2f ? new Vector4(0.8f, 0.8f, 0.2f, 1f) : new Vector4(0.8f, 0.2f, 0.2f, 1f));
            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, ImGui.ColorConvertFloat4ToU32(color));
            ImGui.ProgressBar(percent, new Vector2(-1, 0), $"{current:N0}");
            ImGui.PopStyleColor();
        }

        private Vector4 GetDamageColor(DamageType type)
        {
            return type switch
            {
                DamageType.Magic => new Vector4(0.4f, 0.6f, 1f, 1f),
                DamageType.Darkness => new Vector4(0.6f, 0.2f, 0.8f, 1f),
                DamageType.Healing => new Vector4(0.2f, 1f, 0.2f, 1f),
                _ => new Vector4(1f, 1f, 1f, 1f)
            };
        }

        private void CopyRecapToClipboard()
        {
            if (_selectedRecap == null) return;
            var kb = _selectedRecap.KillingBlow;
            var text = Loc.T("RecapText", _selectedRecap.PlayerName, kb?.SourceName ?? Loc.T("Unknown"), kb?.ActionName ?? Loc.T("Unknown"), kb?.Amount ?? 0);
            ImGui.SetClipboardText(text);
        }

        private void PushKayTheme()
        {
            ImGui.PushStyleColor(ImGuiCol.WindowBg, ImGui.ColorConvertFloat4ToU32(new Vector4(0.08f, 0.08f, 0.1f, 0.95f)));
            ImGui.PushStyleColor(ImGuiCol.TitleBgActive, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.15f, 0.18f, 1.0f)));
            ImGui.PushStyleColor(ImGuiCol.Button, ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.15f, 0.18f, 1.0f)));
            ImGui.PushStyleColor(ImGuiCol.ButtonHovered, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.7f, 0.75f, 1.0f)));
            ImGui.PushStyleColor(ImGuiCol.ButtonActive, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.95f, 1.0f, 1.0f)));
            ImGui.PushStyleColor(ImGuiCol.Header, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.3f, 0.35f, 1.0f)));
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.6f, 0.65f, 1.0f)));
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, ImGui.ColorConvertFloat4ToU32(new Vector4(0.01f, 0.95f, 1.0f, 1.0f)));
            ImGui.PushStyleColor(ImGuiCol.FrameBg, ImGui.ColorConvertFloat4ToU32(new Vector4(0.15f, 0.15f, 0.16f, 1.0f)));
            ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, 8f);
            ImGui.PushStyleVar(ImGuiStyleVar.FrameRounding, 4f);
        }

        private void PopKayTheme()
        {
            ImGui.PopStyleColor(9);
            ImGui.PopStyleVar(2);
        }
    }
}
