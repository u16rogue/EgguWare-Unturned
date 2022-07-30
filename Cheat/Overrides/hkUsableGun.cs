using EgguWare.Attributes;
using EgguWare.Cheats;
using EgguWare.Classes;
using EgguWare.Utilities;
using SDG.NetTransport;
using SDG.Unturned;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace EgguWare.Overrides
{
    [Comp]
    public class hkUsableGun : MonoBehaviour
    {
        private static FieldInfo BulletsField = typeof(UseableGun).GetField("bullets", BindingFlags.NonPublic | BindingFlags.Instance);
        private static FieldInfo AttachmentsField = typeof(UseableGun).GetField("thirdAttachments", BindingFlags.NonPublic | BindingFlags.Instance);

        // adding an override to only send the silent aim raycast if the bullets are at the end of their travel
        public static void OV_ballistics(UseableGun instance)
        {
            UseableGun PlayerUse = instance;
            var player = instance.player;

            if (Time.realtimeSinceStartup - PlayerLifeUI.hitmarkers[0].lastHit > PlayerUI.HIT_TIME)
            {
                PlayerLifeUI.hitmarkers[0].image.isVisible = false;
            }

            ItemGunAsset PAsset = (ItemGunAsset)Player.player.equipment.asset;
            PlayerLook Look = Player.player.look;

            if (PAsset.projectile != null)
                return;

            List<BulletInfo> Bullets = (List<BulletInfo>)BulletsField.GetValue(PlayerUse);

            if (Bullets.Count == 0)
                return;

            if (instance.channel.isOwner)
            {
                Transform t = (player.look.perspective == EPlayerPerspective.FIRST ? player.look.aim : G.MainCamera.transform);
                RaycastInfo ri = hkDamageTool.SetupRaycast(new Ray(t.position, t.forward), T.GetGunDistance().Value, RayMasks.DAMAGE_CLIENT);
                if (Provider.modeConfigData.Gameplay.Ballistics)
                {
                    for (int i = 0; i < Bullets.Count; i++)
                    {
                        BulletInfo bulletInfo = Bullets[i];
                        double distance = Vector3.Distance(player.transform.position, ri.point);

                        if (bulletInfo.steps * PAsset.ballisticTravel < distance)
                            continue;

                        EPlayerHit eplayerhit = CalcHitMarker(PAsset, ref ri);
                        PlayerUI.hitmark(0, Vector3.zero, false, eplayerhit);
                        player.input.sendRaycast(ri, ERaycastInfoUsage.Gun);
                        Weapons.AddTracer(ri);
                        Weapons.AddDamage(ri);
                        bulletInfo.steps = 254;
                    }
                }
                else
                {
                    for (int i = 0; i < Bullets.Count; i++)
                    {
                        EPlayerHit eplayerhit = CalcHitMarker(PAsset, ref ri);
                        PlayerUI.hitmark(0, Vector3.zero, false, eplayerhit);
                        player.input.sendRaycast(ri, ERaycastInfoUsage.Gun);
                        Weapons.AddTracer(ri);
                        Weapons.AddDamage(ri);
                    }
                }
            }

            if (Provider.isServer)
            {
                while (Bullets.Count > 0)
                {
                    BulletInfo bulletInfo3 = Bullets[0];
                    byte pellets2 = bulletInfo3.magazineAsset.pellets;
                    if (!player.input.hasInputs())
                    {
                        break;
                    }
                    InputInfo input = player.input.getInput(true, ERaycastInfoUsage.Gun);
                    if (input == null || PAsset == null)
                    {
                        break;
                    }
                    if (!instance.channel.isOwner)
                    {
                        if (Provider.modeConfigData.Gameplay.Ballistics)
                        {
                            if ((input.point - bulletInfo3.pos).magnitude > PAsset.ballisticTravel * (float)((long)(bulletInfo3.steps + 1) + (long)((ulong)PlayerInput.SAMPLES)) + 4f)
                            {
                                Bullets.RemoveAt(0);
                                continue;
                            }
                        }
                        else if ((input.point - player.look.aim.position).sqrMagnitude > (PAsset.range + 4f) * (PAsset.range + 4f))
                        {
                            break;
                        }
                    }
                    /*
                    if (UseableGun.onBulletHit != null)
                    {
                        bool flag = true;
                        UseableGun.onBulletHit(this, bulletInfo3, input, ref flag);
                        if (!flag)
                        {
                            Bullets.RemoveAt(0);
                            continue;
                        }
                    }
                    */
                    if (!string.IsNullOrEmpty(input.materialName))
                    {
                        if (bulletInfo3.magazineAsset != null && bulletInfo3.magazineAsset.impact != 0)
                        {
                            DamageTool.impact(input.point, input.normal, bulletInfo3.magazineAsset.impact, instance.channel.owner.playerID.steamID, instance.transform.position);
                        }
                        else
                        {
                            ServerSpawnBulletImpact(input.point, input.normal, input.materialName, input.colliderTransform, instance.channel.EnumerateClients_WithinSphereOrOwner(input.point, EffectManager.SMALL));
                        }
                    }
                    EPlayerKill eplayerKill = EPlayerKill.NONE;
                    uint num4 = 0U;
                    float num5 = getBulletDamageMultiplier(instance, ref bulletInfo3);
                    float num6 = Vector3.Distance(bulletInfo3.origin, input.point);
                    float num7 = Mathf.InverseLerp(PAsset.range * PAsset.damageFalloffRange, PAsset.range, num6);
                    num5 *= Mathf.Lerp(1f, PAsset.damageFalloffMultiplier, num7);
                    ERagdollEffect useableRagdollEffect = player.equipment.getUseableRagdollEffect();
                    if (input.type == ERaycastInfoType.PLAYER)
                    {
                        if (input.player != null && (DamageTool.isPlayerAllowedToDamagePlayer(player, input.player) || PAsset.bypassAllowedToDamagePlayer))
                        {
                            bool flag2 = input.limb == ELimb.SKULL && PAsset.instakillHeadshots && Provider.modeConfigData.Players.Allow_Instakill_Headshots;
                            IDamageMultiplier playerDamageMultiplier = PAsset.playerDamageMultiplier;
                            DamagePlayerParameters damagePlayerParameters = DamagePlayerParameters.make(input.player, EDeathCause.GUN, input.direction * Mathf.Ceil((float)pellets2 / 2f), playerDamageMultiplier, input.limb);
                            damagePlayerParameters.killer = instance.channel.owner.playerID.steamID;
                            damagePlayerParameters.times = num5;
                            damagePlayerParameters.respectArmor = !flag2;
                            damagePlayerParameters.trackKill = true;
                            damagePlayerParameters.ragdollEffect = useableRagdollEffect;
                            PAsset.initPlayerDamageParameters(ref damagePlayerParameters);
                            if (player.input.IsUnderFakeLagPenalty)
                            {
                                damagePlayerParameters.times *= Provider.configData.Server.Fake_Lag_Damage_Penalty_Multiplier;
                            }
                            DamageTool.damagePlayer(damagePlayerParameters, out eplayerKill);
                        }
                    }
                    else if (input.type == ERaycastInfoType.ZOMBIE)
                    {
                        if (input.zombie != null)
                        {
                            bool flag3 = input.limb == ELimb.SKULL && PAsset.instakillHeadshots && Provider.modeConfigData.Zombies.Weapons_Use_Player_Damage && Provider.modeConfigData.Players.Allow_Instakill_Headshots;
                            Vector3 vector = input.direction * Mathf.Ceil((float)pellets2 / 2f);
                            IDamageMultiplier zombieOrPlayerDamageMultiplier = PAsset.zombieOrPlayerDamageMultiplier;
                            DamageZombieParameters damageZombieParameters = DamageZombieParameters.make(input.zombie, vector, zombieOrPlayerDamageMultiplier, input.limb);
                            damageZombieParameters.times = num5 * input.zombie.getBulletResistance();
                            damageZombieParameters.allowBackstab = false;
                            damageZombieParameters.respectArmor = !flag3;
                            damageZombieParameters.instigator = player;
                            damageZombieParameters.ragdollEffect = useableRagdollEffect;
                            DamageTool.damageZombie(damageZombieParameters, out eplayerKill, out num4);
                            if (player.movement.nav != 255)
                            {
                                input.zombie.alert(instance.transform.position, true);
                            }
                        }
                    }
                    else if (input.type == ERaycastInfoType.ANIMAL)
                    {
                        if (input.animal != null)
                        {
                            Vector3 vector2 = input.direction * Mathf.Ceil((float)pellets2 / 2f);
                            IDamageMultiplier animalOrPlayerDamageMultiplier = PAsset.animalOrPlayerDamageMultiplier;
                            DamageAnimalParameters damageAnimalParameters = DamageAnimalParameters.make(input.animal, vector2, animalOrPlayerDamageMultiplier, input.limb);
                            damageAnimalParameters.times = num5;
                            damageAnimalParameters.instigator = player;
                            damageAnimalParameters.ragdollEffect = useableRagdollEffect;
                            DamageTool.damageAnimal(damageAnimalParameters, out eplayerKill, out num4);
                            input.animal.alertDamagedFromPoint(instance.transform.position);
                        }
                    }
                    else if (input.type == ERaycastInfoType.VEHICLE)
                    {
                        if (input.vehicle != null && input.vehicle.asset != null && input.vehicle.canBeDamaged && (input.vehicle.asset.isVulnerable || ((ItemWeaponAsset)player.equipment.asset).isInvulnerable))
                        {
                            float num8 = (PAsset.isInvulnerable ? Provider.modeConfigData.Vehicles.Gun_Highcal_Damage_Multiplier : Provider.modeConfigData.Vehicles.Gun_Lowcal_Damage_Multiplier);
                            DamageTool.damage(input.vehicle, true, input.point, false, PAsset.vehicleDamage, num5 * num8, true, out eplayerKill, instance.channel.owner.playerID.steamID, EDamageOrigin.Useable_Gun);
                        }
                    }
                    else if (input.type == ERaycastInfoType.BARRICADE)
                    {
                        if (input.transform != null && input.transform.CompareTag("Barricade"))
                        {
                            BarricadeDrop barricadeDrop2 = BarricadeManager.FindBarricadeByRootTransform(input.transform);
                            if (barricadeDrop2 != null)
                            {
                                ItemBarricadeAsset asset3 = barricadeDrop2.asset;
                                if (asset3 != null && asset3.canBeDamaged && (asset3.isVulnerable || ((ItemWeaponAsset)player.equipment.asset).isInvulnerable))
                                {
                                    float num9 = (PAsset.isInvulnerable ? Provider.modeConfigData.Barricades.Gun_Highcal_Damage_Multiplier : Provider.modeConfigData.Barricades.Gun_Lowcal_Damage_Multiplier);
                                    DamageTool.damage(input.transform, false, PAsset.barricadeDamage, num5 * num9, out eplayerKill, instance.channel.owner.playerID.steamID, EDamageOrigin.Useable_Gun);
                                }
                            }
                        }
                    }
                    else if (input.type == ERaycastInfoType.STRUCTURE)
                    {
                        if (input.transform != null && input.transform.CompareTag("Structure"))
                        {
                            StructureDrop structureDrop2 = StructureManager.FindStructureByRootTransform(input.transform);
                            if (structureDrop2 != null)
                            {
                                ItemStructureAsset asset4 = structureDrop2.asset;
                                if (asset4 != null && asset4.canBeDamaged && (asset4.isVulnerable || ((ItemWeaponAsset)player.equipment.asset).isInvulnerable))
                                {
                                    float num10 = (PAsset.isInvulnerable ? Provider.modeConfigData.Structures.Gun_Highcal_Damage_Multiplier : Provider.modeConfigData.Structures.Gun_Lowcal_Damage_Multiplier);
                                    DamageTool.damage(input.transform, false, input.direction * Mathf.Ceil((float)pellets2 / 2f), PAsset.structureDamage, num5 * num10, out eplayerKill, instance.channel.owner.playerID.steamID, EDamageOrigin.Useable_Gun);
                                }
                            }
                        }
                    }
                    else if (input.type == ERaycastInfoType.RESOURCE)
                    {
                        byte b3;
                        byte b4;
                        ushort num11;
                        if (input.transform != null && input.transform.CompareTag("Resource") && ResourceManager.tryGetRegion(input.transform, out b3, out b4, out num11))
                        {
                            ResourceSpawnpoint resourceSpawnpoint2 = ResourceManager.getResourceSpawnpoint(b3, b4, num11);
                            if (resourceSpawnpoint2 != null && !resourceSpawnpoint2.isDead && PAsset.hasBladeID(resourceSpawnpoint2.asset.bladeID))
                            {
                                DamageTool.damage(input.transform, input.direction * Mathf.Ceil((float)pellets2 / 2f), PAsset.resourceDamage, num5, 1f, out eplayerKill, out num4, instance.channel.owner.playerID.steamID, EDamageOrigin.Useable_Gun);
                            }
                        }
                    }
                    else if (input.type == ERaycastInfoType.OBJECT && input.transform != null && input.section < 255)
                    {
                        InteractableObjectRubble componentInParent2 = input.transform.GetComponentInParent<InteractableObjectRubble>();
                        if (componentInParent2 != null && !componentInParent2.isSectionDead(input.section) && (componentInParent2.asset.rubbleIsVulnerable || ((ItemWeaponAsset)player.equipment.asset).isInvulnerable))
                        {
                            DamageTool.damage(componentInParent2.transform, input.direction, input.section, PAsset.objectDamage, num5, out eplayerKill, out num4, instance.channel.owner.playerID.steamID, EDamageOrigin.Useable_Gun);
                        }
                    }
                    if (input.type != ERaycastInfoType.PLAYER && input.type != ERaycastInfoType.ZOMBIE && input.type != ERaycastInfoType.ANIMAL && !player.life.isAggressor)
                    {
                        float num12 = PAsset.range + Provider.modeConfigData.Players.Ray_Aggressor_Distance;
                        num12 *= num12;
                        float num13 = Provider.modeConfigData.Players.Ray_Aggressor_Distance;
                        num13 *= num13;
                        Vector3 normalized = (bulletInfo3.pos - player.look.aim.position).normalized;
                        for (int j = 0; j < Provider.clients.Count; j++)
                        {
                            if (Provider.clients[j] != instance.channel.owner)
                            {
                                Player player2 = Provider.clients[j].player;
                                if (!(player2 == null))
                                {
                                    Vector3 vector3 = player2.look.aim.position - player2.look.aim.position;
                                    Vector3 vector4 = Vector3.Project(vector3, normalized);
                                    if (vector4.sqrMagnitude < num12 && (vector4 - vector3).sqrMagnitude < num13)
                                    {
                                        player2.life.markAggressive(false, true);
                                    }
                                }
                            }
                        }
                    }
                    if (Level.info.type == ELevelType.HORDE)
                    {
                        if (input.zombie != null)
                        {
                            if (input.limb == ELimb.SKULL)
                            {
                                player.skills.askPay(10U);
                            }
                            else
                            {
                                player.skills.askPay(5U);
                            }
                        }
                        if (eplayerKill == EPlayerKill.ZOMBIE)
                        {
                            if (input.limb == ELimb.SKULL)
                            {
                                player.skills.askPay(50U);
                            }
                            else
                            {
                                player.skills.askPay(25U);
                            }
                        }
                    }
                    else
                    {
                        if (eplayerKill == EPlayerKill.PLAYER && Level.info.type == ELevelType.ARENA)
                        {
                            player.skills.askPay(100U);
                        }
                        player.sendStat(eplayerKill);
                        if (num4 > 0U)
                        {
                            player.skills.askPay(num4);
                        }
                    }
                    Vector3 vector5 = input.point + input.normal * 0.25f;
                    if (bulletInfo3.magazineAsset != null && bulletInfo3.magazineAsset.isExplosive)
                    {
                        EffectManager.triggerEffect(new TriggerEffectParameters(bulletInfo3.magazineAsset.explosion)
                        {
                            position = vector5,
                            relevantDistance = EffectManager.MEDIUM,
                            wasInstigatedByPlayer = true
                        });
                        List<EPlayerKill> list;
                        DamageTool.explode(new ExplosionParameters(vector5, bulletInfo3.magazineAsset.range, EDeathCause.SPLASH, instance.channel.owner.playerID.steamID)
                        {
                            playerDamage = bulletInfo3.magazineAsset.playerDamage,
                            zombieDamage = bulletInfo3.magazineAsset.zombieDamage,
                            animalDamage = bulletInfo3.magazineAsset.animalDamage,
                            barricadeDamage = bulletInfo3.magazineAsset.barricadeDamage,
                            structureDamage = bulletInfo3.magazineAsset.structureDamage,
                            vehicleDamage = bulletInfo3.magazineAsset.vehicleDamage,
                            resourceDamage = bulletInfo3.magazineAsset.resourceDamage,
                            objectDamage = bulletInfo3.magazineAsset.objectDamage,
                            damageOrigin = EDamageOrigin.Bullet_Explosion,
                            ragdollEffect = useableRagdollEffect,
                            launchSpeed = bulletInfo3.magazineAsset.explosionLaunchSpeed
                        }, out list);
                        foreach (EPlayerKill eplayerKill2 in list)
                        {
                            player.sendStat(eplayerKill2);
                        }
                    }
                    if (bulletInfo3.dropID != 0)
                    {
                        ItemManager.dropItem(new Item(bulletInfo3.dropID, bulletInfo3.dropAmount, bulletInfo3.dropQuality), vector5, false, Dedicator.IsDedicatedServer, false);
                    }
                    Bullets.RemoveAt(0);
                }
            }

            if (player.equipment.asset != null)
            {
                if (Provider.modeConfigData.Gameplay.Ballistics)
                {
                    for (int k = Bullets.Count - 1; k >= 0; k--)
                    {
                        BulletInfo bulletInfo4 = Bullets[k];
                        bulletInfo4.steps += 1;
                        if (bulletInfo4.steps >= PAsset.ballisticSteps)
                        {
                            Bullets.RemoveAt(k);
                        }
                    }
                    return;
                }
                Bullets.Clear();
            }
        }

        public static EPlayerHit CalcHitMarker(ItemGunAsset PAsset, ref RaycastInfo ri)
        {
            EPlayerHit eplayerhit = EPlayerHit.NONE;

            if (ri == null || PAsset == null)
                return eplayerhit;

            if (ri.animal || ri.player || ri.zombie)
            {
                eplayerhit = EPlayerHit.ENTITIY;
                if (ri.limb == ELimb.SKULL)
                    eplayerhit = EPlayerHit.CRITICAL;
            }
            else if (ri.transform)
            {
                if (ri.transform.CompareTag("Barricade") && PAsset.barricadeDamage > 1f)
                {
                    InteractableDoorHinge component = ri.transform.GetComponent<InteractableDoorHinge>();
                    if (component != null)
                        ri.transform = component.transform.parent.parent;

                    if (!ushort.TryParse(ri.transform.name, out ushort id)) return eplayerhit;

                    ItemBarricadeAsset itemBarricadeAsset = (ItemBarricadeAsset)Assets.find(EAssetType.ITEM, id);

                    if (itemBarricadeAsset == null || !itemBarricadeAsset.isVulnerable && !PAsset.isInvulnerable)
                        return eplayerhit;

                    if (eplayerhit == EPlayerHit.NONE)
                        eplayerhit = EPlayerHit.BUILD;
                }
                else if (ri.transform.CompareTag("Structure") && PAsset.structureDamage > 1f)
                {
                    if (!ushort.TryParse(ri.transform.name, out ushort id2)) return eplayerhit;

                    ItemStructureAsset itemStructureAsset = (ItemStructureAsset)Assets.find(EAssetType.ITEM, id2);

                    if (itemStructureAsset == null || !itemStructureAsset.isVulnerable && !PAsset.isInvulnerable)
                        return eplayerhit;

                    if (eplayerhit == EPlayerHit.NONE)
                        eplayerhit = EPlayerHit.BUILD;
                }
                else if (ri.transform.CompareTag("Resource") && PAsset.resourceDamage > 1f)
                {
                    if (!ResourceManager.tryGetRegion(ri.transform, out byte x, out byte y, out ushort index))
                        return eplayerhit;

                    ResourceSpawnpoint resourceSpawnpoint = ResourceManager.getResourceSpawnpoint(x, y, index);

                    if (resourceSpawnpoint == null || resourceSpawnpoint.isDead ||
                         !PAsset.bladeIDs.Contains(resourceSpawnpoint.asset.bladeID)) return eplayerhit;

                    if (eplayerhit == EPlayerHit.NONE)
                        eplayerhit = EPlayerHit.BUILD;
                }
                else if (PAsset.objectDamage > 1f)
                {
                    InteractableObjectRubble component2 = ri.transform.GetComponent<InteractableObjectRubble>();

                    if (component2 == null) return eplayerhit;

                    ri.section = component2.getSection(ri.collider.transform);

                    if (component2.isSectionDead(ri.section) ||
                        !component2.asset.rubbleIsVulnerable && !PAsset.isInvulnerable) return eplayerhit;

                    if (eplayerhit == EPlayerHit.NONE)
                        eplayerhit = EPlayerHit.BUILD;
                }
            }
            else if (ri.vehicle && !ri.vehicle.isDead && PAsset.vehicleDamage > 1f)
                if (ri.vehicle.asset != null && (ri.vehicle.asset.isVulnerable || PAsset.isInvulnerable))
                    if (eplayerhit == EPlayerHit.NONE)
                        eplayerhit = EPlayerHit.BUILD;

            return eplayerhit;
        }

        private static float getBulletDamageMultiplier(UseableGun instance, ref BulletInfo bullet)
        {
            float num = (bullet.quality < 0.5f) ? (0.5f + bullet.quality) : 1f;
            if (bullet.magazineAsset != null)
            {
                num *= bullet.magazineAsset.ballisticDamageMultiplier;
            }

            var attach = (Attachments)AttachmentsField.GetValue(instance);

            if (attach.sightAsset != null)
            {
                num *= attach.sightAsset.ballisticDamageMultiplier;
            }
            if (attach.tacticalAsset != null)
            {
                num *= attach.tacticalAsset.ballisticDamageMultiplier;
            }
            if (attach.barrelAsset != null)
            {
                num *= attach.barrelAsset.ballisticDamageMultiplier;
            }
            if (attach.gripAsset != null)
            {
                num *= attach.gripAsset.ballisticDamageMultiplier;
            }
            return num;
        }

        internal static void ServerSpawnBulletImpact(Vector3 position, Vector3 normal, string materialName, Transform colliderTransform, IEnumerable<ITransportConnection> transportConnections)
        {
            position += normal * UnityEngine.Random.Range(0.04f, 0.06f);
            SendSpawnBulletImpact.Invoke(ENetReliability.Unreliable, transportConnections, position, normal, materialName, colliderTransform);
        }

        private static ClientStaticMethod<Vector3, Vector3, string, Transform> SendSpawnBulletImpact = ClientStaticMethod<Vector3, Vector3, string, Transform>.Get(new ClientStaticMethod<Vector3, Vector3, string, Transform>.ReceiveDelegate(DamageTool.ReceiveSpawnBulletImpact));
    }
}
