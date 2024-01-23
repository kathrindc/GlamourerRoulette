using System;
using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Dalamud.Interface.Internal.Notifications;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace GlamourerRoulette
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Glamourer Roulette";

        private DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        public Configuration Configuration { get; init; }

        private Random Random { get; init; }

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager)
        {
            this.PluginInterface = pluginInterface;
            this.CommandManager = commandManager;

            this.Configuration = this.PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            this.Configuration.Initialize(this.PluginInterface);

            if (!PluginInterface.InstalledPlugins.Select(p => p.Name).Contains("Glamourer"))
            {
                PluginInterface.UiBuilder.AddNotification("Glamourer is either not installed or unavailable for other reasons. Please check your xlplugins.", "Glamourer Roulette", NotificationType.Error, 8000U);
                throw new Exception("glamourer unavailable");
            }

            Random = new Random();

            this.CommandManager.AddHandler("/gr", new CommandInfo(OnPickRandom)
            {
                HelpMessage = "Picks a random outfit from your saved Glamourer designs"
            });
        }

        public void Dispose()
        {
            this.CommandManager.RemoveHandler("/gr");
        }

        private void OnPickRandom(string command, string args)
        {
            var getDesignList = PluginInterface.GetIpcSubscriber<(string, Guid)[]>("Glamourer.GetDesignList");
            var applyByGuid = PluginInterface.GetIpcSubscriber<Guid, string, object>("Glamourer.ApplyByGuid");
            var designs = getDesignList.InvokeFunc();

            if (!designs.Any())
            {
                PluginInterface.UiBuilder.AddNotification("Could not find any saved Glamourer designs", "Glamourer Roulette", NotificationType.Warning, 8000U);
                return;
            }
            
            var index = Random.Next(0, designs.Length - 1);
            var design = designs[index].Item2;
            var character = GetCurrentCharacterName();
            
            applyByGuid.InvokeAction(design, character);
        }

        private static unsafe string GetCurrentCharacterName()
        {
            var playerStatePtr = PlayerState.Instance();
            var nameBuffer = new byte[43];
            
            Marshal.Copy((IntPtr)playerStatePtr->CharacterName, nameBuffer, 0, 43);

            return Encoding.Default.GetString(nameBuffer).Replace("\0", "");
        }
    }
}
