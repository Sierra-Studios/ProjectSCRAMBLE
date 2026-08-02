using System;
using AbosulteCustomItems.API.Features.BaseItemFolder;
using Exiled.API.Features;
using Exiled.CustomItems.API;

using ProjectSCRAMBLE.Configs;

namespace ProjectSCRAMBLE
{
    public class Plugin : Plugin<Config>
    {
        public static Plugin Instance { get; private set; }

        public EventHandlers EventHandlers { get; private set; }

        public override string Author { get; } = "MS";

        public override string Name { get; } = "ProjectSCRAMBLE";

        public override string Prefix { get; } = "ProjectSCRAMBLE";

        public override Version Version { get; } = new Version(1, 5, 0);

        public override Version RequiredExiledVersion { get; } = new Version(9, 13, 0);

        public override void OnEnabled()
        {
            Instance = this;
            EventHandlers = new EventHandlers();
            BaseItem.RegisterAll(Assembly, null);
            base.OnEnabled();
        }

        public override void OnDisabled()
        {
            BaseItem.UnregisterAll(Assembly);

            EventHandlers = null;
            Instance = null;
            base.OnDisabled();
        }
    }
}
