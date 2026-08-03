using System;
using HarmonyLib;
using SecureCookingPots.src.managers;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace SecureCookingPots;

public class MainSystem : ModSystem
{
    private Harmony? harmony;
    private const string ModName = "securecookingpots";
    private const string HarmonyId = $"com.furio.{ModName}";
    private int disposed;

    public IClientNetworkChannel? ClientChannel { get; private set; }
    public IServerNetworkChannel? ServerChannel { get; private set; }

    public SlotFlashManager? FlashManager { get; private set; }
    private PunishmentManager? punishmentManager;

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
            api.Logger.Error($"[{ModName}] Failed to apply Harmony patches! (e: {ex})");
        }
    }

    public override void StartServerSide(ICoreServerAPI api) {
        base.StartServerSide(api);
        
        punishmentManager = new PunishmentManager(api);
        
        ServerChannel = api.Network.RegisterChannel(ModName)
            .RegisterMessageType<PunishMessage>()
            .SetMessageHandler<PunishMessage>(punishmentManager.OnPunishMessageReceived);
    }

    public override void StartClientSide(ICoreClientAPI api) {
        base.StartClientSide(api);
        
        ClientChannel = api.Network.RegisterChannel(ModName)
            .RegisterMessageType<PunishMessage>();
        
        FlashManager = new SlotFlashManager(api);
    }

    public override void Dispose() {
        if (System.Threading.Interlocked.Exchange(ref disposed, 1) == 1) return;

        harmony?.UnpatchAll(HarmonyId);
        harmony = null;

        FlashManager?.Dispose();
        FlashManager = null;
        punishmentManager?.Dispose();
        punishmentManager = null;
        
        ClientChannel = null;
        ServerChannel = null;
        
        base.Dispose();
    }
}