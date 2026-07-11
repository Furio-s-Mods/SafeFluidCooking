using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace SafeFluidCooking;

[HarmonyPatch(typeof(InventoryBase), nameof(InventoryBase.ActivateSlot))]
public class PreventContainerRemovalPatch {

    [HarmonyPrefix]
    static bool Prefix(InventoryBase __instance, int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op) {
        if (__instance?.Api?.Logger == null) return true;
        if (__instance is not InventorySmelting smeltingInv || slotId != 1) return true;

        var cookingSlots = smeltingInv.CookingSlots;
        if (cookingSlots == null || cookingSlots.Length == 0) return true;

        foreach (var innerSlot in cookingSlots) {
            var innerStack = innerSlot?.Itemstack;
            if (innerStack == null) continue;

            var props = BlockLiquidContainerBase.GetContainableProps(innerStack);
            if (props == null) continue;

            float currentTemp = innerStack.Collectible.GetTemperature(__instance.Api.World, innerStack);
            bool isHot = currentTemp >= 60f;

            HandleFailureEffects(__instance.Api, __instance[slotId], op, __instance.InventoryID, isHot);
            return false; 
        }
        return true;
    }
    
    private static void HandleFailureEffects(ICoreAPI api, ItemSlot slot, ItemStackMoveOperation op, string inventoryId, bool isHot) {
        // --- Client Side Feedback ---
        if (api is ICoreClientAPI capi) {
            capi.ShowChatMessage("Spilling this would make a mess. Empty it first.");
            
            var mainSystem = capi.ModLoader.GetModSystem<MainSystem>();
            mainSystem?.TriggerRedFlash(slot, 0.4f);
            
            if (isHot) {
                mainSystem?.ClientChannel?.SendPacket(new PunishMessage { InventoryId = inventoryId });
            }
            else {
                capi.World.PlaySoundAt(
                    location: new AssetLocation("game:sounds/held/bookclose3"),
                    atEntity: capi.World.Player.Entity,
                    randomizePitch: true,
                    range: 8f,
                    volume: 1.0f
                );
            }
        }
        
        // --- Server Side Fallback Check (Exploit Guard) ---
        if (api is ICoreServerAPI) {
            if (isHot) {
                MainSystem.ApplyPunishment(op.ActingPlayer);
            }
        }
    }
}