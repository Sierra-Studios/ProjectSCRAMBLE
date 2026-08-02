using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using AbosulteCustomItems.API.Events.EventArgs;
using AbosulteCustomItems.API.Features;
using AbosulteCustomItems.API.Features.BaseItemFolder;
using AbosulteCustomItems.API.Features.BaseItemFolder.GenericDefinition;
using AbosulteCustomItems.API.Features.Custom914;
using Exiled.API.Enums;
using Exiled.API.Features;
using Exiled.API.Features.Items;
using Exiled.API.Features.Spawn;
using Exiled.CustomItems.API.EventArgs;
using Exiled.CustomItems.API.Features;
using Exiled.Events.EventArgs.Player;
using Exiled.Events.EventArgs.Scp096;
using Exiled.Events.EventArgs.Scp1344;
using Exiled.Events.EventArgs.Scp914;
using InventorySystem.Items.Usables.Scp1344;
using MEC;

using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.Wearables;

using ProjectSCRAMBLE.Configs;
using ProjectSCRAMBLE.Extensions;

using UnityEngine;

using YamlDotNet.Serialization;

using static ProjectSCRAMBLE.Methods;

using Scp96Event = Exiled.Events.Handlers.Scp096;

namespace ProjectSCRAMBLE
{
    public class ScrambleShebang : Shebang
    {
        public float Charge { get; set; } = 100f;
    }
    
    public class ProjectSCRAMBLE : BaseItem<ScrambleShebang>
    {
        //internal static ProjectSCRAMBLE<> SCRAMBLE { get; private set; }

        [YamlIgnore]
        public static HashSet<Player> ActiveScramblePlayers { get; } = [];

        public override string Name { get; init; } = "Project SCRAMBLE";
        public override int Id { get; init; } = 1730;

        public override ItemType Type { get; init; } = ItemType.SCP1344;
        public override float? Weight { get; init; } = 1;

        //Config values:
        public float WearingTime { get; init; } = 1;
        public float RemovingTime { get; init; } = 1;
        [Description("Should there be a Random error in the artificial intelligence of the glasses?")]
        public bool RandomError { get; init; } = false;

        [Description("Random error chance")]
        public float RandomErrorChance { get; init; } = 0.001f;

        [Description("Whether the SCRAMBLES will use charge while blocking SCP-096 face")]
        public bool ScrambleCharge { get; init; } = true;

        [Description("How much power should the SCRAMBLEs use to obfuscate 96's face? (1 = default, >1 = faster, <1 = slower)")]
        public float ChargeUsageMultiplayer { get; init; } = 1;

        [Description("Attach to head or Directly attach to player")]
        public bool AttachCensorToHead { get; init; } = true;

        [Description("0.1 is okey, 0.01 better/good , 0.001 greater")]
        public float AttachToHeadsyncInterval { get; init; } = 0.01f;
        
        [Description("Censor type as primitive")]
        public PrimitiveType CensorType { get; init; } = PrimitiveType.Cube;

        [Description("Rotate censor randomly")]
        public bool CensorRotate { get; init; } = true;

        [Description("Censor Color")]
        public Color CensorColor { get; init; } = new Color(0, 0, 0, 1);

        [Description("Censor scale")]
        public Vector3 CensorScale { get; init; } = Vector3.one * 0.5f;
        
        public string Charge { get; init; } = "<color=green>Project SCRAMBLE ACTIVE charge status: {charge}</color>";
        public string OffCharge { get; init; } = "<color=red>SCRAMBLE = !WARNING!! CHARGE-OFF</color>";
        public string Error { get; init; } = "<color=red>SCRAMBLE = ?? !WARNING!! CRITICAL ERROR</color>";
        public float HintTime { get; init; } = 5;
        
        protected override void OnRegistered(RegisteredEventArgs registeredEventArgs)
        {
            Exiled.Events.Handlers.Scp1344.ChangedStatus += OnChangedStatus;
            Exiled.Events.Handlers.Scp1344.Deactivated += OnDeactivated;
            Exiled.Events.Handlers.Scp914.UpgradingPickup += UpgradingPickup;
            Exiled.Events.Handlers.Scp914.UpgradingInventoryItem += UpgradingInventoryItem;
            Scp96Event.AddingTarget += OnAddingTarget;
        }

        protected override void OnUnregistered()
        {
            Exiled.Events.Handlers.Scp1344.ChangedStatus -= OnChangedStatus;
            Exiled.Events.Handlers.Scp1344.Deactivated -= OnDeactivated;
            Exiled.Events.Handlers.Scp914.UpgradingPickup -= UpgradingPickup;
            Exiled.Events.Handlers.Scp914.UpgradingInventoryItem -= UpgradingInventoryItem;
            Scp96Event.AddingTarget -= OnAddingTarget;
        }

        public void OnChangedStatus(ChangedStatusEventArgs ev)
        {
            if (GetInstance(ev.Item)?.Template is not ProjectSCRAMBLE)
            {
                return;
            }
            
            switch (ev.Scp1344Status)
            {
                case Scp1344Status.Activating:
                    ev.Scp1344.Base._useTime = 5f - WearingTime;
                    break;
                case Scp1344Status.Active:
                    OnActive(ev.Player, ev.Scp1344);
                    break;
                case Scp1344Status.Deactivating:
                    ev.Scp1344.Base._useTime = 5.1f - RemovingTime;
                    break;
            }
        }
        
        protected void OnActive(Player player, Scp1344 goggles)
        {
            ItemInstance instance = GetInstance(goggles.Serial);
            ScrambleShebang shebang = (ScrambleShebang)instance!.Storage;
            if (ScrambleCharge)
            {
                if (shebang.Charge <= 0f)
                {
                    player.DisableEffect(EffectType.Scp1344);
                    player.AddSCRAMBLEHint(GetOriginalDefinition<ProjectSCRAMBLE>()!.OffCharge);
                    player.ReferenceHub.EnableWearables(WearableElements.Scp1344Goggles);
                    Log.Debug($"{player.Nickname}: Tried to wear SCRAMBLE with no charge.");
                    return;
                }

                string hint = GetOriginalDefinition<ProjectSCRAMBLE>()!.Charge.Replace("{charge}", shebang.Charge.FormatCharge());
                player.ShowHint(hint, GetOriginalDefinition<ProjectSCRAMBLE>()!.HintTime);

                Log.Debug($"{player.Nickname}: SCRAMBLEs charge {shebang.Charge}.");
            }

            ObfuscateScp96s(player);
            ActiveScramblePlayers.Add(player);

            foreach (Player ply in player.CurrentSpectatingPlayers)
            {
                ObfuscateScp96s(ply);
                Plugin.Instance.EventHandlers.DirtyPlayers.Add(ply);
            }

            Log.Debug($"{player.Nickname}: Activated Project Scramble");
        }
        
        private void OnDeactivated(DeactivatedEventArgs ev)
        {
            RemoveFor(ev.Player);
        }

        public void RemoveFor(Player player)
        {
            DeObfuscateScp96s(player);
            ActiveScramblePlayers.Remove(player);

            foreach (Player ply in player.CurrentSpectatingPlayers)
            {
                DeObfuscateScp96s(ply);
                Plugin.Instance.EventHandlers.DirtyPlayers.Remove(ply);
            }

            Log.Debug($"{player.Nickname} : Deactivated  Project Scramble");
        }

        protected override void OnItemRemoved(ItemInstance item, ScrambleShebang shebang, in ItemRemovedEventArgs ev)
        {
            RemoveFor(item.OwnerManaged);
            base.OnItemRemoved(item, shebang, in ev);
        }

        protected override void OnTemplateRoundRestart()
        {
            ActiveScramblePlayers.Clear();
            base.OnTemplateRoundRestart();
        }
        

        public void OnUpgrading(UpgradingEventArgs ev)
        {
           
        }
        
        private void UpgradingPickup(UpgradingPickupEventArgs ev)
        {
            if (GetInstance(ev.Pickup)?.Storage is not ScrambleShebang scrambleShebang)
            {
                return;
            }
            
            switch(ev.KnobSetting)
            {
                case Scp914.Scp914KnobSetting.Rough:
                    scrambleShebang.Charge = 0f;
                    break;

                case Scp914.Scp914KnobSetting.Coarse:
                    float charge = Random.Range(0, 50f);
                    scrambleShebang.Charge = charge;
                    break;

                case Scp914.Scp914KnobSetting.Fine:
                case Scp914.Scp914KnobSetting.VeryFine:
                    scrambleShebang.Charge = 100f;
                    break;
            }
        }

        private void UpgradingInventoryItem(UpgradingInventoryItemEventArgs ev)
        {
            if (GetInstance(ev.Item)?.Storage is not ScrambleShebang scrambleShebang)
            {
                return;
            }
            
            switch(ev.KnobSetting)
            {
                case Scp914.Scp914KnobSetting.Rough:
                    scrambleShebang.Charge = 0f;
                    break;

                case Scp914.Scp914KnobSetting.Coarse:
                    float charge = Random.Range(0, 50f);
                    scrambleShebang.Charge = charge;
                    break;

                case Scp914.Scp914KnobSetting.Fine:
                case Scp914.Scp914KnobSetting.VeryFine:
                    scrambleShebang.Charge = 100f;
                    break;
            }
        }

        public void OnAddingTarget(AddingTargetEventArgs ev)
        {
            if (!ev.IsLooking)
                return;

            if (!ActiveScramblePlayers.Contains(ev.Target))
                return;

            var translation = BaseItem.GetOriginalDefinition<ProjectSCRAMBLE>()!;

            bool shouldRandomError = RandomError && Random.Range(0f, 100f) <= RandomErrorChance;

            if (!ScrambleCharge)
            {
                if (shouldRandomError)
                {
                    ev.Target.AddSCRAMBLEHint(translation.Error);
                    return;
                }

                ev.IsAllowed = false;
                return;
            }


            var instance = ItemInstance.GetCustomInventory(ev.Player)
                .FirstOrDefault(x => x.Template is ProjectSCRAMBLE);
            var scramble = instance?.Template as ProjectSCRAMBLE;
            var shebang = instance?.Storage as ScrambleShebang;
            
            if (scramble == null)
            {
                Log.Debug
                ($"""
                  [SCRAMBLE ERROR]
                  Player: {ev.Target.Nickname} ({ev.Target.UserId})
                  Reason: No matching SCRAMBLE serial found.
                  """);
                ev.IsAllowed = false;
                return;
            }
            
            if (shebang!.Charge <= 0f)
            {
                ev.Target.AddSCRAMBLEHint(translation.OffCharge);
                DeObfuscateScp96s(ev.Target);
                return;
            }

            shebang.Charge -= Time.deltaTime * ChargeUsageMultiplayer;

            if (shouldRandomError)
            {
                ev.Target.AddSCRAMBLEHint(translation.Error);
                Timing.CallDelayed(0.5f, () => ev.Target.AddSCRAMBLEHint(translation.Charge.Replace("{charge}", shebang.Charge.FormatCharge())));
                return;
            }

            ev.Target.AddSCRAMBLEHint(translation.Charge.Replace("{charge}", shebang.Charge.FormatCharge()));
            ev.IsAllowed = false;
        }

        public void ObfuscateScp96s(Player player)
        {
            foreach (GameObject censor in Scp96Censors.Values)
            {
                player.ShowHidedNetworkObject(censor);
            }
        }

        public void DeObfuscateScp96s(Player player)
        {
            foreach (GameObject censor in Scp96Censors.Values)
            {
                player.HideNetworkObject(censor);
            }
        }
    }
}
