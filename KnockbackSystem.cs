using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;

namespace KnockbackSystem;

public class KnockbackSystem : BasePlugin
{
    public override string ModuleName => "Knockback System";
    public override string ModuleVersion => "0.0.1";
    public override string ModuleAuthor => "belom0r";

    public static Dictionary<CCSPlayerController, float> VelocityModifier = new Dictionary<CCSPlayerController, float>();

    public override void Load(bool hotReload)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);
        RegisterEventHandler<EventPlayerHurt>(PlayerHurtPre, HookMode.Pre);
    }

    public override void Unload(bool hotReload)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
    }

    private HookResult OnTakeDamage(DynamicHook handle)
    {
        CEntityInstance victim = handle.GetParam<CEntityInstance>(0);
        CCSPlayerController? Victim = victim.player();

        CCSPlayerPawn? PlayerPawn = Victim.PlayerPawn();

        if (PlayerPawn == null)
            return HookResult.Continue;

        VelocityModifier.Add(Victim, PlayerPawn.VelocityModifier);

        return HookResult.Continue;
    }

    private HookResult PlayerHurtPre(EventPlayerHurt @event, GameEventInfo info)
    {
        CCSPlayerController? Victim = @event.Userid;

        CCSPlayerPawn? PlayerPawn = Victim.PlayerPawn();

        if (PlayerPawn == null)
            return HookResult.Continue;

        if (VelocityModifier.ContainsKey(Victim))
        {
            PlayerPawn.VelocityModifier = VelocityModifier[Victim];
            VelocityModifier.Remove(Victim);
        }

        return HookResult.Continue;
    }
}

public static class GeneralPurpose
{
    // https://github.com/destoer/Cs2Jailbreak/blob/master/src/Lib.cs#L419
    public static CCSPlayerController? player(this CEntityInstance? instance)
    {
        if (instance == null)
        {
            return null;
        }

        // grab the pawn index
        int player_index = (int)instance.Index;

        // grab player controller from pawn
        CCSPlayerPawn? player_pawn = Utilities.GetEntityFromIndex<CCSPlayerPawn>(player_index);

        // pawn valid
        if (player_pawn == null || !player_pawn.IsValid)
        {
            return null;
        }

        // controller valid
        if (player_pawn.OriginalController == null || !player_pawn.OriginalController.IsValid)
        {
            return null;
        }

        // any further validity is up to the caller
        return player_pawn.OriginalController.Value;
    }

    public static CCSPlayerPawn? PlayerPawn(this CCSPlayerController? PlayerController)
    {
        if (PlayerController == null || !PlayerController.IsValid || !PlayerController.PlayerPawn.IsValid)
        {
            return null;
        }

        CCSPlayerPawn? PlayerPawn = PlayerController.PlayerPawn.Value;

        return PlayerPawn;
    }
}
