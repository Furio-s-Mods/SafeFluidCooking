using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Vintagestory.GameContent;
using ProtoBuf;

namespace SecureCookingPots.src.managers;

[ProtoContract]
public class PunishMessage 
{
    [ProtoMember(1)]
    public string InventoryId { get; set; }
}

public class PunishmentManager(ICoreServerAPI sapi)
{
    private ICoreServerAPI sapi = sapi;

    private static readonly DamageSource FireDamage = new() {
        Source = EnumDamageSource.Internal,
        Type = EnumDamageType.Fire 
    };


    /// <summary>
    /// Unified check to see if a cooking pot is dangerously hot.
    /// </summary>
    public static bool IsPotHot(IWorldAccessor world, InventorySmelting inv)
    {
        var potStack = inv?.Slots[1]?.Itemstack;
        if (potStack == null) return false;

        float currentTemp = potStack.Collectible.GetTemperature(world, potStack);
        return currentTemp >= 60f;
    }

    public void OnPunishMessageReceived(IServerPlayer player, PunishMessage msg) 
    {
        if (player?.InventoryManager == null || string.IsNullOrEmpty(msg.InventoryId) || sapi == null) return;

        if (player.InventoryManager.GetInventory(msg.InventoryId) is InventorySmelting inv)
        {
            if (IsPotHot(sapi.World, inv)) 
            {
                ApplyDamage(player);
            }
        }
    }

    public static void ApplyDamage(IPlayer player) 
    {
        if (player?.Entity == null) return;
        if (player.WorldData.CurrentGameMode != EnumGameMode.Survival) return;

        player.Entity.ReceiveDamage(FireDamage, 1.0f);

        AssetLocation soundLocation = new("game:sounds/effect/extinguish1");
        player.Entity.World.PlaySoundAt(
            location: soundLocation, 
            atEntity: player.Entity, 
            randomizePitch: true,
            range: 16f,
            volume: 1.0f
        );
    }

    /// <summary>
    /// Unloads server API references to prevent memory leaks during mod suspension.
    /// </summary>
    public void Dispose()
    {
        sapi = null;
    }
}