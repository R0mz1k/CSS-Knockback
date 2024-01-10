using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Memory;
using CounterStrikeSharp.API.Modules.Memory.DynamicFunctions;
using CounterStrikeSharp.API.Modules.Utils;

namespace Knockback;

public class Knockback : BasePlugin
{
    public override string ModuleName => "Knockback System";
    public override string ModuleVersion => "0.0.1";
    public override string ModuleAuthor => "belom0r";

    public static Dictionary<CCSPlayerController, float> VelocityModifier = new Dictionary<CCSPlayerController, float>();
    public static Dictionary<CCSPlayerController, Vector> DamageDirection = new Dictionary<CCSPlayerController, Vector>();

    public static Dictionary<string, float> KBWeaponPower = new Dictionary<string, float>()
    {
        {"weapon_glock", 1.0f},
        {"weapon_usp_silencer", 1.0f},
        {"weapon_hkp2000", 1.0f},
        {"weapon_elite", 1.0f},
        {"weapon_p250", 1.0f},
        {"weapon_fiveseven", 1.0f},
        {"weapon_cz75a", 1.0f},
        {"weapon_deagle", 1.0f},
        {"weapon_revolver", 1.0f},
        {"weapon_nova", 1.0f},
        {"weapon_xm1014", 1.0f},
        {"weapon_sawedoff", 1.0f},
        {"weapon_mag7", 1.0f},
        {"weapon_m249", 1.0f},
        {"weapon_negev", 1.0f},
        {"weapon_mac10", 1.0f},
        {"weapon_mp7", 1.0f},
        {"weapon_mp9", 1.0f},
        {"weapon_mp5sd", 1.0f},
        {"weapon_ump45", 1.0f},
        {"weapon_p90", 1.0f},
        {"weapon_bizon", 1.0f},
        {"weapon_galilar", 1.0f},
        {"weapon_famas", 1.0f},
        {"weapon_ak47", 1.0f},
        {"weapon_m4a4", 1.0f},
        {"weapon_m4a1_silencer", 1.0f},
        {"weapon_ssg08", 1.0f},
        {"weapon_sg556", 1.0f},
        {"weapon_aug", 1.0f},
        {"weapon_awp", 1.0f},
        {"weapon_g3sg1", 1.0f},
        {"weapon_scar20", 1.0f}
    };

    public override void Load(bool hotReload)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Post);

        RegisterEventHandler<EventPlayerHurt>(PlayerHurtPost, HookMode.Post);
    }

    public override void Unload(bool hotReload)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Unhook(OnTakeDamage, HookMode.Pre);
    }

    private HookResult OnTakeDamage(DynamicHook handle)
    {
        CEntityInstance victim = handle.GetParam<CEntityInstance>(0);

        CTakeDamageInfo damage_info = handle.GetParam<CTakeDamageInfo>(1);

        CCSPlayerController? Victim = victim.player();

        CCSPlayerPawn? PlayerPawn = Victim.PlayerPawn();

        if (Victim == null || PlayerPawn == null)
            return HookResult.Continue;

        VelocityModifier.Add(Victim, PlayerPawn.VelocityModifier);

        if (damage_info.BitsDamageType == 2)
        {
            DamageDirection.Add(Victim, damage_info.DamageDirection);
        }

        return HookResult.Continue;
    }

    private HookResult PlayerHurtPost(EventPlayerHurt @event, GameEventInfo info)
    {
        CCSPlayerController? Victim = @event.Userid;
        CCSPlayerPawn? VictimPawn = Victim.PlayerPawn();

        if (VictimPawn == null)
            return HookResult.Continue;

        if (VelocityModifier.ContainsKey(Victim))
        {
            VictimPawn.VelocityModifier = VelocityModifier[Victim];
            VelocityModifier.Remove(Victim);
        }

        CCSPlayerController? Attacker = @event.Attacker;
        CCSPlayerPawn? AttackerPawn = Attacker.PlayerPawn();

        string Weapon = @event.Weapon;

        if (DamageDirection.ContainsKey(Victim))
        {
            float knockback_distance = 0f;

            if (AttackerPawn != null)
            {
                Vector VictimPosition = VictimPawn.AbsOrigin ?? new Vector(0f, 0f, 0f);
                Vector AttackerPosition = AttackerPawn.AbsOrigin ?? new Vector(0f, 0f, 0f);

                knockback_distance = GeneralPurpose.Distance(VictimPosition, AttackerPosition);
            }

            if (knockback_distance > 350)
                return HookResult.Continue;

            Vector VictimVelocity = VictimPawn.AbsVelocity;
            Vector vecDamageDirection = DamageDirection[Victim];

            DamageDirection.Remove(Victim);

            string WeaponName = $"weapon_{Weapon}";

            if (KBWeaponPower.ContainsKey(WeaponName))
                vecDamageDirection = vecDamageDirection.Multiply(250 * KBWeaponPower[WeaponName]);
            else
                vecDamageDirection = vecDamageDirection.Multiply(250);

            vecDamageDirection.Z = VictimVelocity.Z;
            VictimPawn.AbsVelocity.Add(vecDamageDirection);
        }

        return HookResult.Continue;
    }
}

public static class GeneralPurpose
{
    public static Vector Multiply(this Vector vector, float skl)
    {
        var x = vector.X * skl;
        var y = vector.Y * skl;
        var z = vector.Z * skl;

        return new Vector(x, y, z);
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

    // https://discord.com/channels/1160907911501991946/1175947333880524962/1191763347549458514
    public static float Distance(Vector point1, Vector point2)
    {
        float dx = point2.X - point1.X;
        float dy = point2.Y - point1.Y;
        float dz = point2.Z - point1.Z;

        return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

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
}
