using System;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using ProtoBuf;
using Vintagestory.GameContent;

namespace SafeFluidCooking;

[ProtoContract]
public class PunishMessage 
{
    [ProtoMember(1)]
    public string InventoryId { get; set; }
}

public class MainSystem : ModSystem
{
    private Harmony harmony;
    const string modName = "safefluidcooking";
    private const string HarmonyId = $"com.furio.{modName}";
    private int disposed;

    private ICoreClientAPI capi;
    private ItemSlot activeFlashingSlot;
    private float flashRemainingTime;
    private long tickListenerId;

    public IClientNetworkChannel ClientChannel { get; private set; }
    public IServerNetworkChannel ServerChannel { get; private set; }

    public override bool ShouldLoad(EnumAppSide forSide) => true;

    public override void Start(ICoreAPI api) {
        base.Start(api);
        
        try
        {
            harmony = new Harmony(HarmonyId);
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            // api.Logger.Notification($"[{modName}] Harmony patches applied successfully!");
        }
        catch (Exception ex)
        {
            api.Logger.Error($"[{modName}] Failed to apply Harmony patches! (e: {ex}");
        }
    }

    public override void StartServerSide(ICoreServerAPI api) {
        base.StartServerSide(api);
        
        ServerChannel = api.Network.RegisterChannel(modName)
            .RegisterMessageType<PunishMessage>()
            .SetMessageHandler<PunishMessage>(OnPunishMessageReceived);
    }

    public override void StartClientSide(ICoreClientAPI api) {
        base.StartClientSide(api);
        capi = api;
        
        ClientChannel = api.Network.RegisterChannel(modName)
            .RegisterMessageType<PunishMessage>();
        
        api.Event.LeaveWorld += OnLeaveWorld;
    }

    private void OnPunishMessageReceived(IServerPlayer player, PunishMessage msg) {
        if (player?.InventoryManager == null || string.IsNullOrEmpty(msg.InventoryId)) return;

        var inv = player.InventoryManager.GetInventory(msg.InventoryId) as InventorySmelting;
        if (inv?.CookingSlots == null) return;

        bool verifiedHot = false;
        foreach (var slot in inv.CookingSlots) {
            var stack = slot?.Itemstack;
            if (stack == null) continue;

            if (BlockLiquidContainerBase.GetContainableProps(stack) != null) {
                if (stack.Collectible.GetTemperature(player.Entity.World, stack) >= 60f) {
                    verifiedHot = true;
                    break;
                }
            }
        }

        if (verifiedHot) {
            ApplyPunishment(player);
        }
    }

    public static void ApplyPunishment(IPlayer player) {
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

    private void OnLeaveWorld() {
        ResetActiveSlot();
        StopTickListener();
    }

    private void OnClientGameTick(float deltaTime) {
        if (activeFlashingSlot == null) {
            StopTickListener();
            return;
        }

        if (activeFlashingSlot.Inventory == null || activeFlashingSlot.Inventory.openedByPlayerGUIds.Count == 0) {
            ResetActiveSlot();
            StopTickListener();
            return;
        }

        flashRemainingTime -= deltaTime;

        if (flashRemainingTime <= 0f) {
            ResetActiveSlot();
            StopTickListener();
        }
    }

    public void TriggerRedFlash(ItemSlot slot, float duration) {
        if (slot == null) return;

        ResetActiveSlot();

        activeFlashingSlot = slot;
        activeFlashingSlot.HexBackgroundColor = "#A32424";
        flashRemainingTime = duration;

        if (tickListenerId == 0) {
            tickListenerId = capi.Event.RegisterGameTickListener(OnClientGameTick, 20);
        }
    }

    private void ResetActiveSlot() {
        if (activeFlashingSlot != null) {
            activeFlashingSlot.HexBackgroundColor = null;
            activeFlashingSlot = null;
        }
    }

    private void StopTickListener() {
        if (tickListenerId != 0) {
            capi.Event.UnregisterGameTickListener(tickListenerId);
            tickListenerId = 0;
        }
    }

    public override void Dispose() {
        if (System.Threading.Interlocked.Exchange(ref disposed, 1) == 1) return;

        harmony?.UnpatchAll(HarmonyId);
        harmony = null;

        capi?.Event.LeaveWorld -= OnLeaveWorld;
        capi = null;
        
        ClientChannel = null;
        ServerChannel = null;
        
        StopTickListener();
        
        base.Dispose();
    }
}