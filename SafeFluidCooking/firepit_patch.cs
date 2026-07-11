using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;

namespace SafeFluidCooking;

[HarmonyPatch(typeof(InventoryBase), nameof(InventoryBase.ActivateSlot))]
public class PreventContainerRemovalPatch {
    
    [HarmonyPrefix]
    static bool Prefix(InventoryBase __instance, int slotId, ItemSlot sourceSlot, ref ItemStackMoveOperation op) {
        
        if (__instance?.Api?.Logger == null) return true;

        if (__instance is InventorySmelting smeltingInv) {

            if (slotId == 1) {

                var cookingSlots = smeltingInv.CookingSlots;

                if (cookingSlots != null && cookingSlots.Length > 0) {
                    
                    for (int i = 0; i < cookingSlots.Length; i++) {
                        var innerSlot = cookingSlots[i];
                        var innerStack = innerSlot?.Itemstack;

                        if (innerStack != null) {

                            var props = BlockLiquidContainerBase.GetContainableProps(innerStack);
                            
                            if (props != null) {                                
                                // __instance.Api.Logger.Notification(" !! ACTION BLOCKED !!");

                                if (__instance.Api is ICoreClientAPI capi) {
                                    capi.TriggerChatMessage("Spilling this would make a mess. Empty it first.");
                                    
                                    var flashSystem = capi.ModLoader.GetModSystem<MainSystem>();
                                    
                                    flashSystem?.TriggerRedFlash(__instance[slotId], 0.4f);
                                }
                                return false;
                            }
                        }
                    }
                }
            }
        }

        return true;
    }
}
