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
        private static Player aimtarget = null;
        private static InputSimulator lol = new InputSimulator();
        void Update()
        {
            // Toggle aiming
            if (G.Settings.AimbotOptions.Aimlock)
            {
                KeyCode k = G.Settings.AimbotOptions.AimLockOnScope ? KeyCode.Mouse1 : G.Settings.AimbotOptions.AimlockKey;
                if (Input.GetKeyDown(k))
                    Aiming = true;
                else if (Input.GetKeyUp(k))
                    Aiming = false;

            }
            else if (Aiming) // if aimlock was turned off and aiming was on
            {
                Aiming = false;
            }

            Player local = Player.player;
            UseableGun ug = Player.player?.equipment?.useable as UseableGun;
            bool equip_busy = local.equipment.isBusy;

            // rest trigger
            if (autofired_last && (Menu.Main.MenuOpen || aimtarget is null || Cursor.visible || equip_busy || ug is null))
            {
                lol.Mouse.LeftButtonUp();
                autofired_last = false;
            }

            aimtarget = null; // reset

            // Check if aimbot can target player
            if (Aiming)
            {
                int? fov = null;
                if (G.Settings.AimbotOptions.aim_fov != 0)
                    fov = G.Settings.AimbotOptions.aim_fov;
                Player t = T.GetNearestPlayer(fov, (int)T.GetGunDistance());
                if (t != null)
                    aimtarget = t;
            }

            // if no target is found try if silaim can
            RaycastInfo ri;
            if (aimtarget is null && Overrides.hkDamageTool.SilAimRaycast(out ri, true) && ri.player != null)
                aimtarget = ri.player;

            // no target, abort
            if (aimtarget is null)
                return;

            // Trace check
            if (G.Settings.AimbotOptions.TraceCheck && !T.VisibleFromCamera(aimtarget.transform.position))
                return;

            // If we were aimbotting start locking here
            if (Aiming)
                T.AimAt(T.GetLimbPosition(aimtarget.transform, "Skull"));

            // Abort if we are already auto firing
            if (autofired_last)
                return;

            // Check if we should auto fire, if auto fire is off but target is marked, override it
            bool should_af = G.Settings.AimbotOptions.AutoFire || T.GetPriority(T.GetSteamPlayer(aimtarget).playerID.steamID.m_SteamID) == Classes.Priority.Marked;
            if (should_af && !equip_busy && !Menu.Main.MenuOpen)
            {
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
