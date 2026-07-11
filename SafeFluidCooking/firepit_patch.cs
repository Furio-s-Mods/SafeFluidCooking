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

            HandleFailureEffects(__instance.Api, __instance[slotId], op);
            return false; 
        }
        return true;
    }
    
    private static void HandleFailureEffects(ICoreAPI api, ItemSlot slot, ItemStackMoveOperation op) {
        // --- Client Side Feedback ---
        if (api is ICoreClientAPI capi) {
            capi.ShowChatMessage("Spilling this would make a mess. Empty it first.");
            
            var mainSystem = capi.ModLoader.GetModSystem<MainSystem>();
            mainSystem?.TriggerRedFlash(slot, 0.4f);
            mainSystem?.ClientChannel?.SendPacket(new PunishMessage());
        }
        
        // --- Server Side Fallback Check ---
        if (api is ICoreServerAPI) {
            MainSystem.ApplyPunishment(op.ActingPlayer);
        }
    }
}