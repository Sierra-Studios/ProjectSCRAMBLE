using System.Collections.Generic;
using AbosulteCustomItems.API.Features.BaseItemFolder;
using AdminToys;

using Exiled.API.Features;
using Exiled.API.Features.Roles;
using Exiled.API.Features.Toys;

using MEC;

using Mirror;

using PlayerRoles;

#if PMER
using ProjectMER.Features;
using ProjectMER.Features.Extensions;
using ProjectMER.Features.Objects;
#endif

using ProjectSCRAMBLE.Configs;
using ProjectSCRAMBLE.Extensions;

using UnityEngine;

namespace ProjectSCRAMBLE
{
    public class Methods
    {
        internal static Dictionary<Player, GameObject> Scp96Censors { get; private set; } = [];

        internal static Dictionary<Player, HashSet<CoroutineHandle>> Coroutines { get; private set; } = [];

        internal static void AddCensor(Player player)
        {
            if (player.Role.Type != RoleTypeId.Scp096)
                return;

            if (!player.TryGetScp96Head(out Transform head))
            {
                Log.Error("Scp096 head not found.");
                return;
            }

            if (Scp96Censors.ContainsKey(player))
                RemoveCensor(player);

            var config = BaseItem.GetOriginalDefinition<ProjectSCRAMBLE>()!;

            Primitive Censor = Primitive.Create(primitiveType: config.CensorType, flags: PrimitiveFlags.Visible, position: head.position,
                rotation: head.rotation.eulerAngles, scale: config.CensorScale, spawn: true, color: config.CensorColor);

            Censor.MovementSmoothing = 0;
            Censor.Transform.SetParent(player.Transform, true);

            if ((config.AttachCensorToHead || config.CensorRotate) && !Coroutines.ContainsKey(player))
                Coroutines[player] = []; 

            if (config.AttachCensorToHead)
                Coroutines[player].Add(Timing.RunCoroutine(TrackHead(Censor.Transform, head, config.AttachToHeadsyncInterval)));

            if (config.CensorRotate)
                Coroutines[player].Add(Timing.RunCoroutine(RotateRandom(Censor.Transform)));

            Scp96Censors.Add(player, Censor.GameObject);
            HideForUnGlassesPlayer(Censor.GameObject);
        }

        internal static void RemoveCensor(Player player)
        {
            if (!Scp96Censors.ContainsKey(player))
                return;

            GameObject Censor = Scp96Censors[player];

            Scp96Censors.Remove(player);
            NetworkServer.Destroy(Censor);

            foreach (CoroutineHandle handle in Coroutines[player])
            {
                Timing.KillCoroutines(handle);
            }
        }

        private static void HideForUnGlassesPlayer(GameObject gameObject)
        {
            HashSet<Player> activeScramblePlayers = ProjectSCRAMBLE.ActiveScramblePlayers;

            foreach (Player ply in Player.List)
            {
                if (activeScramblePlayers.Contains(ply))
                    continue;

                if (ply.Role is SpectatorRole spcRole && activeScramblePlayers.Contains(spcRole.SpectatedPlayer))
                {
                    Plugin.Instance.EventHandlers.DirtyPlayers.Add(spcRole.SpectatedPlayer);
                    continue;
                }

                ply.HideNetworkObject(gameObject);
            }
        }

        private static IEnumerator<float> TrackHead(Transform censor, Transform head, float syncinterval)
        {
            while (censor != null && head != null)
            {
                censor.position = head.position;

                yield return syncinterval;
            }
        }

        private static IEnumerator<float> RotateRandom(Transform tr)
        {
            float t = 0f;

            while (tr != null)
            {
                t += 0.1f;

                float t1 = t * 1.1f;
                float t2 = t * 1.7f;
                float t3 = t * 2.3f;

                float x = Mathf.Sin(t1) * 180f;
                float y = Mathf.Cos(t2) * 180f;
                float z = Mathf.Sin(t3) * 180f;

                tr.localRotation = Quaternion.Euler(x, y, z);

                if (t >= 1000f)
                    t = 0f;

                yield return 0.1f;
            }
        }
    }
}
