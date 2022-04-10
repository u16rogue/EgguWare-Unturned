using EgguWare.Attributes;
using EgguWare.Utilities;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WindowsInput;
using UnityEngine;

namespace EgguWare.Cheats
{
    [Comp]
    public class Aimlock : MonoBehaviour
    {
        public static bool Aiming = false;
        static bool autofired_last = false;
        public static InputSimulator lol = new InputSimulator();
        void Update()
        {
            // should aimlock?
            if (G.Settings.AimbotOptions.Aimlock)
            {
                KeyCode aimkey = G.Settings.AimbotOptions.AimLockOnScope ? KeyCode.Mouse1 : G.Settings.AimbotOptions.AimlockKey;

                if (Input.GetKeyDown(aimkey))
                    Aiming = true;
                if (Input.GetKeyUp(aimkey))
                    Aiming = false;
            }
            else if (Aiming)
            {
                Aiming = false;
            }

            // Target selection
            if (Aiming || G.Settings.AimbotOptions.SilentAim)
            {
                int? fov = null;
                if (G.Settings.AimbotOptions.aim_fov != 0)
                    fov = G.Settings.AimbotOptions.aim_fov;
                G.aim_target = T.GetNearestPlayer(fov, (int)T.GetGunDistance());
            }
            else if (G.aim_target != null)
            {
                G.aim_target = null;
            }

            UseableGun ug = Player.player?.equipment?.useable as UseableGun;
            if (ug != null && autofired_last && (G.aim_target == null || Cursor.visible || Player.player.equipment.isBusy))
            {
                lol.Mouse.LeftButtonUp();
                autofired_last = false;
            }

            // -------------------------------------------------------

            // aimlock
            if (Aiming && G.aim_target != null)
            {
                Vector3 HeadPos = T.GetLimbPosition(G.aim_target.transform, "Skull");
                T.AimAt(HeadPos);
            }

            // auto fire
            if (G.aim_target != null && ug != null && !Cursor.visible && (G.Settings.AimbotOptions.AutoFire || G.Settings.AimbotOptions.RageOnMarkedPlayers && T.GetPriority(T.GetSteamPlayer(G.aim_target).playerID.steamID.m_SteamID) == Classes.Priority.Marked))
            {
                Player local = Player.player;
                if (local.equipment.isBusy)
                    return;

                lol.Mouse.LeftButtonDown();
                autofired_last = true;
            }
        }
        void OnGUI()
        {
            if (!G.BeingSpied && Provider.isConnected)
            {
                if (G.Settings.AimbotOptions.AimlockDrawFOV && G.Settings.AimbotOptions.AimlockLimitFOV && G.Settings.AimbotOptions.Aimlock)
                    T.DrawCircle(Colors.GetColor("Aimlock_FOV_Circle"), new Vector2(Screen.width / 2, Screen.height / 2), G.Settings.AimbotOptions.AimlockFOV);
                if (G.Settings.AimbotOptions.SilentAimDrawFOV && G.Settings.AimbotOptions.SilentAim && G.Settings.AimbotOptions.SilentAimLimitFOV)
                    T.DrawCircle(Colors.GetColor("Silent_Aim_FOV_Circle"), new Vector2(Screen.width / 2, Screen.height / 2), G.Settings.AimbotOptions.SilentAimFOV);

                if (autofired_last)
                {
                    T.DrawOutlineLabel(new Vector2(20, 20), Color.white, Color.black, "shooting", "shooting");
                }
            }
        }
    }
}
