using System;
using System.Collections.Generic;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using GlamourerRoulette.Windows;

namespace GlamourerRoulette
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Glamourer Roulette";

        private DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }
        public WindowSystem WindowSystem = new("GlamourerRoulette");
        
        public ConfigWindow ConfigWindow { get; init; }

        public string LastAppliedOutfit { get; private set; } = "N/A";

        private Random Random { get; init; }
        private static int LastRandomNumber = -1; 
        
        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            Random = new Random();

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            if (!IsGlamourerAvailable())
            {
                PluginInterface.UiBuilder.AddNotification("Glamourer is either not installed or unavailable for other reasons. Please check your xlplugins.", "Glamourer Roulette", NotificationType.Error, 8000U);
            }

            ConfigWindow = new ConfigWindow(this);
            
            WindowSystem.AddWindow(ConfigWindow);

            CommandManager.AddHandler("/gr", new CommandInfo(OnPickRandom)
            {
                HelpMessage = "Picks a random outfit from your saved Glamourer designs"
            });

            CommandManager.AddHandler("/grconfig", new CommandInfo(OnOpenConfig)
            {
                HelpMessage = "Opens the configuration window for Glamourer Roulette"
            });

            PluginInterface.UiBuilder.Draw += Draw;
            PluginInterface.UiBuilder.OpenConfigUi += OpenConfig;
        }

        private void Draw()
        {
            WindowSystem.Draw();
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            ConfigWindow.Dispose();
            
            CommandManager.RemoveHandler("/gr");
            CommandManager.RemoveHandler("/grconfig");
        }

        private bool IsGlamourerAvailable() =>
            PluginInterface.InstalledPlugins.Select(p => p.Name).Contains("Glamourer");

        private void OnOpenConfig(string command, string args) => OpenConfig();

        private void OpenConfig()
        {
            if (!IsGlamourerAvailable())
            {
                PluginInterface.UiBuilder.AddNotification("Glamourer is either not installed or unavailable for other reasons. Please check your xlplugins.", "Glamourer Roulette", NotificationType.Error, 8000U);
                return;
            }
            
            ConfigWindow.IsOpen = true;
        }
        
        private void OnPickRandom(string command, string args)
        {
            if (!IsGlamourerAvailable())
            {
                PluginInterface.UiBuilder.AddNotification("Glamourer is either not installed or unavailable for other reasons. Please check your xlplugins.", "Glamourer Roulette", NotificationType.Error, 8000U);
                return;
            }
            
            var designs = GetDesigns().Where(d => d.Enabled).ToList();

            if (!designs.Any())
            {
                PluginInterface.UiBuilder.AddNotification("Could not find any saved Glamourer designs", "Glamourer Roulette", NotificationType.Warning, 8000U);
                return;
            }
            
            var index = EnsureRandom(designs.Count - 1);

            LastAppliedOutfit = designs[index].Name;
            
            ApplyDesign(designs[index].DesignId);
            
            PluginInterface.UiBuilder.AddNotification($"Switching to design \"{designs[index].Name}\"", "Glamourer Roulette", NotificationType.Info);
        }

        private int EnsureRandom(int upper)
        {
            if (upper == 0)
            {
                return 0;
            }
            
            int value;

            do
            {
                value = Random.Next(0, upper);
            }
            while (value == LastRandomNumber);

            LastRandomNumber = value;

            return value;
        }

        private static unsafe string GetCurrentCharacterName()
        {
            var playerStatePtr = PlayerState.Instance();
            var nameBuffer = new byte[43];
            
            Marshal.Copy((IntPtr)playerStatePtr->CharacterName, nameBuffer, 0, 43);

            return Encoding.Default.GetString(nameBuffer).Replace("\0", "");
        }

        public IEnumerable<DesignPreference> GetDesigns(bool backCheck = false)
        {
            var getDesignList = PluginInterface.GetIpcSubscriber<(string, Guid)[]>("Glamourer.GetDesignList");
            var availableDesigns = getDesignList.InvokeFunc();
            
            ImportDesigns(availableDesigns, backCheck);

            return Configuration.Designs.Values.ToList();
        }

        private void ImportDesigns(IEnumerable<(string, Guid)> list, bool backCheck = false)
        {
            var tuples = list.ToList();
            
            if (backCheck)
            {
                var ids = tuples.Select(t => t.Item2).ToList();
                var remove = Configuration.Designs.Keys
                                          .Where(stored => ids.All(id => id != stored))
                                          .ToList();

                foreach (var id in remove)
                {
                    Configuration.Designs.Remove(id);
                }
            }

            foreach (var entry in tuples)
            {
                if (Configuration.Designs.ContainsKey(entry.Item2))
                {
                    continue;
                }

                var design = new DesignPreference()
                {
                    DesignId = entry.Item2,
                    Name = entry.Item1,
                };

                Configuration.Designs.Add(entry.Item2, design);
            }
        }

        private void ApplyDesign(Guid designId)
        {
            var applyByGuid = PluginInterface.GetIpcSubscriber<Guid, string, object>("Glamourer.ApplyByGuid");
            var character = GetCurrentCharacterName();
            
            applyByGuid.InvokeAction(designId, character);
        }
    }
}
