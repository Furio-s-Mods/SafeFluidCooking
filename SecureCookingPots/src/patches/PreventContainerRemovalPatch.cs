using HarmonyLib;
using SecureCookingPots.src.helpers;
using SecureCookingPots.src.managers;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace SecureCookingPots.src.patches;

[HarmonyPatch(typeof(InventoryBase), nameof(InventoryBase.ActivateSlot))]
public class PreventContainerRemovalPatch {

    [HarmonyPrefix]
    static bool Prefix(InventoryBase __instance, int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op) {
        if (__instance?.Api?.Logger == null) return true;
        if (__instance is not InventorySmelting smeltingInv || slotId != 1) return true;

        var cookingSlots = smeltingInv.CookingSlots;
        if (cookingSlots == null || cookingSlots.Length == 0) return true;

        bool hasContents = false;
        foreach (var innerSlot in cookingSlots) {
            if (innerSlot?.Itemstack != null) {
                hasContents = true;
                break;
            }
        }

        if (!hasContents) return true;

        bool isHot = PunishmentManager.IsPotHot(__instance.Api.World, smeltingInv);

        HandleFailureEffects(__instance.Api, __instance[slotId], op, __instance.InventoryID, isHot);
        return false; 
    }
    
    private static void HandleFailureEffects(ICoreAPI api, ItemSlot slot, ItemStackMoveOperation op, string inventoryId, bool isHot) {
        if (api is ICoreClientAPI capi) {
            // capi.ShowChatMessage("Spilling this would make a mess. Empty it first.");
            
            var mainSystem = capi.ModLoader.GetModSystem<MainSystem>();
            mainSystem?.FlashManager?.TriggerRedFlash(slot, 0.4f);
            
            if (capi.World.Player.WorldData.CurrentGameMode == EnumGameMode.Survival)
            {
                if (isHot) {
                    mainSystem?.ClientChannel?.SendPacket(new PunishMessage { InventoryId = inventoryId });
                }
                else {
                    PlayerSoundHelper.PlayVoiceFeedback(capi.World.Player, EnumTalkType.IdleShort);
                }
            }
        }
        
        if (api is ICoreServerAPI) {
            if (isHot) {
                PunishmentManager.ApplyDamage(op.ActingPlayer);
            }
        }
    }
}