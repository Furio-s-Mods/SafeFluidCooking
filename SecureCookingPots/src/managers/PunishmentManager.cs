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
    private readonly ICoreServerAPI sapi = sapi;

    /// <summary>
    /// Processes incoming exploit guard packets from clients.
    /// </summary>
    public void OnPunishMessageReceived(IServerPlayer player, PunishMessage msg) 
    {
        if (player?.InventoryManager == null || string.IsNullOrEmpty(msg.InventoryId)) return;

        if (player.InventoryManager.GetInventory(msg.InventoryId) is not InventorySmelting inv) return;

        var potStack = inv.Slots[1]?.Itemstack;
        if (potStack == null) return;

        float currentTemp = potStack.Collectible.GetTemperature(sapi.World, potStack);
        
        if (currentTemp >= 60f) 
        {
            ApplyPunishment(player);
        }
    }

    /// <summary>
    /// Inflicts burning damage and triggers an extinguishing sound effect.
    /// </summary>
    public static void ApplyPunishment(IPlayer player) 
    {
        if (player?.Entity == null) return;
        if (player.WorldData.CurrentGameMode != EnumGameMode.Survival) return;

        DamageSource dmgSource = new() {
            Source = EnumDamageSource.Internal,
            Type = EnumDamageType.Fire 
        };
        player.Entity.ReceiveDamage(dmgSource, 1.0f);

        AssetLocation soundLocation = new("game:sounds/effect/extinguish1");
        player.Entity.World.PlaySoundAt(
            location: soundLocation, 
            atEntity: player.Entity, 
            randomizePitch: true,
            range: 16f,
            volume: 1.0f
        );
    }
}