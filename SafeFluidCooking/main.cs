using System;
using HarmonyLib;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace SafeFluidCooking;
public class MainSystem : ModSystem
{
    private Harmony harmony;
    const string modName = "safefluidcooking";
    private const string HarmonyId = $"com.furio.{modName}";
    private bool disposed;

    private ICoreClientAPI capi;
    private ItemSlot activeFlashingSlot;
    private float flashRemainingTime;
    private long tickListenerId;

    public override bool ShouldLoad(EnumAppSide forSide) => true;

    public override void Start(ICoreAPI api) {
        base.Start(api);
        
        try
        {
            harmony = new Harmony(HarmonyId);
            harmony.PatchAll(System.Reflection.Assembly.GetExecutingAssembly());
            api.Logger.Notification($"[{modName}] Harmony patches applied successfully!");
        }
        catch (Exception ex)
        {
            api.Logger.Error($"[{modName}] Failed to apply Harmony patches! (e: {ex}");
        }
    }

    public override void StartClientSide(ICoreClientAPI api) {
        base.StartClientSide(api);
        capi = api;
        
        api.Event.LeaveWorld += OnLeaveWorld;
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
        if (disposed) return;
        disposed = true;

        harmony?.UnpatchAll(HarmonyId);
        harmony = null;

        capi?.Event.LeaveWorld -= OnLeaveWorld;
        capi = null;
        StopTickListener();
        
        base.Dispose();
    }
}