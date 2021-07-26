using EgguWare.Utilities;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace EgguWare.Overrides
{
    public class hkPlayerUI
    {
		private static StaticResourceRef<Texture2D> hitEntityTexture = new StaticResourceRef<Texture2D>("Bundles/Textures/Player/Icons/PlayerLife/Hit_Entity");
		private static StaticResourceRef<Texture2D> hitCriticalTexture = new StaticResourceRef<Texture2D>("Bundles/Textures/Player/Icons/PlayerLife/Hit_Critical");
		private static StaticResourceRef<Texture2D> hitBuildTexture = new StaticResourceRef<Texture2D>("Bundles/Textures/Player/Icons/PlayerLife/Hit_Build");
		private static StaticResourceRef<Texture2D> hitGhostTexture = new StaticResourceRef<Texture2D>("Bundles/Textures/Player/Icons/PlayerLife/Hit_Ghost");
		private static StaticResourceRef<AudioClip> hitCriticalSound = new StaticResourceRef<AudioClip>("Sounds/General/Hit");
		private static FieldInfo wantsWindowEnabled = typeof(PlayerUI).GetField("wantsWindowEnabled", BindingFlags.Static | BindingFlags.NonPublic);

		// hitmarker bonk
		public static void OV_hitmark(int index, Vector3 point, bool worldspace, EPlayerHit newHit)
        {
			if (!(bool)wantsWindowEnabled.GetValue(null))
			{
				return;
			}
			if (index < 0 || index >= PlayerLifeUI.hitmarkers.Length)
			{
				return;
			}
			if (!Provider.modeConfigData.Gameplay.Hitmarkers)
			{
				return;
			}
			HitmarkerInfo hitmarkerInfo = PlayerLifeUI.hitmarkers[index];
			hitmarkerInfo.lastHit = Time.realtimeSinceStartup;
			hitmarkerInfo.hit = newHit;
			hitmarkerInfo.point = point;
			hitmarkerInfo.worldspace = (worldspace || OptionsSettings.hitmarker);
			if (newHit == EPlayerHit.CRITICAL)
			{
				if (G.Settings.WeaponOptions.HitmarkerBonk)
					MainCamera.instance.GetComponent<AudioSource>().PlayOneShot(AssetUtilities.BonkClip, 2 /* volume */);
				else
					MainCamera.instance.GetComponent<AudioSource>().PlayOneShot(hitCriticalSound, 0.5f);
			}
			Texture2D texture;
			Color customColor;
			switch (newHit)
			{
				case EPlayerHit.NONE:
					return;
				case EPlayerHit.ENTITIY:
					texture = hitEntityTexture;
					customColor = OptionsSettings.hitmarkerColor;
					break;
				case EPlayerHit.CRITICAL:
					texture = hitCriticalTexture;
					customColor = OptionsSettings.criticalHitmarkerColor;
					break;
				case EPlayerHit.BUILD:
					texture = hitBuildTexture;
					customColor = OptionsSettings.hitmarkerColor;
					break;
				case EPlayerHit.GHOST:
					texture = hitGhostTexture;
					customColor = OptionsSettings.hitmarkerColor;
					break;
				default:
					return;
			}
			hitmarkerInfo.image.texture = texture;
			hitmarkerInfo.image.color = customColor;
		}
    }
}
