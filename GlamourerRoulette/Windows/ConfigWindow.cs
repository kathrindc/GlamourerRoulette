using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace GlamourerRoulette.Windows;

public class ConfigWindow : Window, IDisposable
{
    private readonly Plugin plugin;
    private List<DesignPreference> list;

    public ConfigWindow(Plugin plugin) : base(
        "Glamourer Roulette Options",
        ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoResize)
    {
        this.plugin = plugin;
        
        Size = new Vector2(300, 600);
        SizeCondition = ImGuiCond.Always;
        list = [];
    }

    public override void OnOpen()
    {
        base.OnOpen();
        
        list = plugin.GetDesigns().OrderBy(d => d.Name).ToList();
    }

    public void Dispose()
    {
    }

    public override void Draw()
    {
        ImGui.PushItemWidth(-1);
        ImGui.BeginListBox("Allowed Designs", new Vector2(-1, -1));
        foreach (var design in list)
        {
            var initialEnabledState = design.Enabled;
            
            if (!initialEnabledState)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.3f, 0.3f, 0.3f, 1.0f));
            }
            
            if (ImGui.Selectable(design.Name, design.Enabled))
            {
                design.Enabled = !design.Enabled;
                plugin.Configuration.Designs[design.DesignId] = design;
                plugin.Configuration.Save();
            }
            
            if (!initialEnabledState)
            {
                ImGui.PopStyleColor();
            }
        }
        ImGui.EndListBox();
        ImGui.PopItemWidth();
    }
}
