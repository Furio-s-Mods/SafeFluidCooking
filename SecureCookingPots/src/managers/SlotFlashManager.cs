using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace SecureCookingPots;

public class SlotFlashManager
{
    private readonly ICoreClientAPI capi;
    private ItemSlot activeFlashingSlot;
    private float flashRemainingTime;
    private long tickListenerId;

    public SlotFlashManager(ICoreClientAPI capi)
    {
        this.capi = capi;
        this.capi.Event.LeaveWorld += OnLeaveWorld;
    }

    public void TriggerRedFlash(ItemSlot slot, float duration)
    {
        if (slot == null) return;

        ResetActiveSlot();

        activeFlashingSlot = slot;
        activeFlashingSlot.HexBackgroundColor = "#A32424";
        flashRemainingTime = duration;

        if (tickListenerId == 0)
        {
            tickListenerId = capi.Event.RegisterGameTickListener(OnClientGameTick, 20);
        }
    }

    private void OnClientGameTick(float deltaTime)
    {
        if (activeFlashingSlot == null)
        {
            StopTickListener();
            return;
        }

        // If the player closed the UI while it was flashing, clean up gracefully
        if (activeFlashingSlot.Inventory == null || activeFlashingSlot.Inventory.openedByPlayerGUIds.Count == 0)
        {
            ResetActiveSlot();
            StopTickListener();
            return;
        }

        flashRemainingTime -= deltaTime;

        if (flashRemainingTime <= 0f)
        {
            ResetActiveSlot();
            StopTickListener();
        }
    }

    private void OnLeaveWorld()
    {
        ResetActiveSlot();
        StopTickListener();
    }

    private void ResetActiveSlot()
    {
        if (activeFlashingSlot != null)
        {
            activeFlashingSlot.HexBackgroundColor = null;
            activeFlashingSlot = null;
        }
    }

    private void StopTickListener()
    {
        if (tickListenerId != 0)
        {
            capi.Event.UnregisterGameTickListener(tickListenerId);
            tickListenerId = 0;
        }
    }

    public void Dispose()
    {
        if (capi?.Event != null)
        {
            capi.Event.LeaveWorld -= OnLeaveWorld;
        }
        ResetActiveSlot();
        StopTickListener();
    }
}