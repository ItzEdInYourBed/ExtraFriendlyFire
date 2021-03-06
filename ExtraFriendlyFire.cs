﻿using Rocket.API;
using Rocket.API.Serialisation;
using Rocket.Core.Plugins;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Logger = Rocket.Core.Logging.Logger;

namespace ExtraConcentratedJuice.ExtraFriendlyFire
{
    class ExtraFriendlyFire : RocketPlugin<ExtraFriendlyFireConfig>
    {
        protected override void Load()
        {
            Logger.Log("ExtraFriendlyFire Loaded!");
            Logger.Log("Groups that will ignore damage from same group: ");
            foreach (string g in Configuration.Instance.groups)
                Logger.Log("\t" + g);
            Logger.Log("Plugin ignores admins: " + Configuration.Instance.ignoreAdmin);
            Logger.Log("Plugin ignores players w/ permission: " + Configuration.Instance.ignorePermissionString);
            DamageTool.playerDamaged += OnPlayerDamage;
        }
        protected override void Unload()
        {
            Logger.Log("Unloading ExtraFriendlyFire...");
            DamageTool.playerDamaged -= OnPlayerDamage;
        }

        private void OnPlayerDamage(Player player, ref EDeathCause cause, ref ELimb limb, ref CSteamID killer, ref Vector3 direction, ref float damage, ref float times, ref bool canDamage)
        {
            UnturnedPlayer victim = UnturnedPlayer.FromPlayer(player);
            UnturnedPlayer attacker = UnturnedPlayer.FromCSteamID(killer);

            if (victim == null || attacker == null)
                return;

            // UnturnedPlayer.HasPermission() will always return true if the player is an admin.
            // We don't want that if ignoreAdmin is set to false.
            // To get around that, we are going to get all permissions of a player and check them in place of HasPermission().
            List<Permission> victimPerms = victim.GetPermissions();
            List<Permission> attackerPerms = attacker.GetPermissions();

            string ignoreString = Configuration.Instance.ignorePermissionString;

            if ((victim.IsAdmin && Configuration.Instance.ignoreAdmin) || victimPerms.Any(x => x.Name == ignoreString))
                return;
            if ((attacker.IsAdmin && Configuration.Instance.ignoreAdmin) || attackerPerms.Any(x => x.Name == ignoreString))
                return;

            List<RocketPermissionsGroup> mutualGroups = GetMutualGroups(victim, attacker);
            List<string> ffGroups = Configuration.Instance.groups;

            for (int i = 0; i < mutualGroups.Count; i++)
            {
                if (ffGroups.Contains(mutualGroups[i].Id))
                {
                    damage = 0;
                    canDamage = false;
                    return;
                }
            }
        }

        public static List<RocketPermissionsGroup> GetMutualGroups(UnturnedPlayer p1, UnturnedPlayer p2)
        {
            List<RocketPermissionsGroup> p1Groups = Rocket.Core.R.Permissions.GetGroups(p1, true);
            List<RocketPermissionsGroup> p2Groups = Rocket.Core.R.Permissions.GetGroups(p2, true);

            return p1Groups.Intersect(p2Groups).ToList();
        }
    }
}
