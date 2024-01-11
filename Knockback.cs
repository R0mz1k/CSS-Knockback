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

    public class WeaponData
    {
        public float Knockback { get; }
        public float MaximumDistance { get; }

        public WeaponData(float Knockback, float MaximumDistance)
        {
            this.Knockback = Knockback;
            this.MaximumDistance = MaximumDistance;
        }
    }

    public Dictionary<string, WeaponData> WeaponDataByWeapons = new Dictionary<string, WeaponData>()
    {
        {"weapon_glock",            new WeaponData( 1.0f, 300.0f ) },
        {"weapon_usp_silencer",     new WeaponData( 1.0f, 300.0f ) },
        {"weapon_hkp2000",          new WeaponData( 1.0f, 300.0f ) },
        {"weapon_elite",            new WeaponData( 1.0f, 300.0f ) },
        {"weapon_p250",             new WeaponData( 1.0f, 300.0f ) },
        {"weapon_fiveseven",        new WeaponData( 1.0f, 300.0f ) },
        {"weapon_cz75a",            new WeaponData( 1.0f, 300.0f ) },
        {"weapon_deagle",           new WeaponData( 1.0f, 300.0f ) },
        {"weapon_revolver",         new WeaponData( 1.0f, 300.0f ) },
        {"weapon_nova",             new WeaponData( 1.0f, 300.0f ) },
        {"weapon_xm1014",           new WeaponData( 6.0f, 300.0f ) },
        {"weapon_sawedoff",         new WeaponData( 1.0f, 300.0f ) },
        {"weapon_mag7",             new WeaponData( 1.0f, 300.0f ) },
        {"weapon_m249",             new WeaponData( 1.0f, 300.0f ) },
        {"weapon_negev",            new WeaponData( 1.0f, 300.0f ) },
        {"weapon_mac10",            new WeaponData( 1.0f, 300.0f ) },
        {"weapon_mp7",              new WeaponData( 1.0f, 300.0f ) },
        {"weapon_mp9",              new WeaponData( 1.0f, 300.0f ) },
        {"weapon_mp5sd",            new WeaponData( 1.0f, 300.0f ) },
        {"weapon_ump45",            new WeaponData( 1.0f, 300.0f ) },
        {"weapon_p90",              new WeaponData( 1.0f, 300.0f ) },
        {"weapon_bizon",            new WeaponData( 1.0f, 200.0f ) },
        {"weapon_galilar",          new WeaponData( 1.0f, 300.0f ) },
        {"weapon_famas",            new WeaponData( 1.0f, 300.0f ) },
        {"weapon_ak47",             new WeaponData( 1.0f, 500.0f ) },
        {"weapon_m4a4",             new WeaponData( 1.0f, 300.0f ) },
        {"weapon_m4a1_silencer",    new WeaponData( 1.0f, 300.0f ) },
        {"weapon_ssg08",            new WeaponData( 1.0f, 300.0f ) },
        {"weapon_sg556",            new WeaponData( 1.0f, 300.0f ) },
        {"weapon_aug",              new WeaponData( 1.0f, 500.0f ) },
        {"weapon_awp",              new WeaponData( 1.0f, 600.0f ) },
        {"weapon_g3sg1",            new WeaponData( 1.0f, 300.0f ) },
        {"weapon_scar20",           new WeaponData( 1.0f, 300.0f ) }
    };

    public class VictimData
    {
        public float VelocityModifier { get; }
        public float Damage { get; }
        public Vector DamageDirection { get; }

        public VictimData(float VelocityModifier, float Damage, Vector DamageDirection)
        {
            this.VelocityModifier = VelocityModifier;
            this.Damage = Damage;
            this.DamageDirection = DamageDirection;
        }
    }

    public Dictionary<CCSPlayerController, VictimData> VictimDataByPlayer = new Dictionary<CCSPlayerController, VictimData>();

    public override void Load(bool hotReload)
    {
        VirtualFunctions.CBaseEntity_TakeDamageOldFunc.Hook(OnTakeDamage, HookMode.Pre);

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

        Vector DamageDirection = damage_info.BitsDamageType == 2 ? damage_info.DamageDirection : new Vector(0f, 0f, 0f);

        VictimDataByPlayer.Add(Victim, new VictimData(PlayerPawn.VelocityModifier, damage_info.Damage, DamageDirection));

        return HookResult.Continue;
    }

    private HookResult PlayerHurtPost(EventPlayerHurt @event, GameEventInfo info)
    {
        CCSPlayerController? Victim = @event.Userid;
        CCSPlayerController? Attacker = @event.Attacker;

        CCSPlayerPawn? VictimPawn = Victim.PlayerPawn();
        CCSPlayerPawn? AttackerPawn = Attacker.PlayerPawn();

        string WeaponName = $"weapon_{@event.Weapon}";

        if (VictimPawn == null || AttackerPawn == null)
            return HookResult.Continue;

        if (!VictimDataByPlayer.ContainsKey(Victim))
            return HookResult.Continue;

        VictimData victimData = VictimDataByPlayer[Victim];

        VictimPawn.VelocityModifier = victimData.VelocityModifier;

        float ratio = 1.0f;

        ratio *= victimData.Damage;

        if (WeaponDataByWeapons.ContainsKey(WeaponName))
        {
            WeaponData weaponData = WeaponDataByWeapons[WeaponName];

            ratio *= weaponData.Knockback;

            Vector VictimPosition = VictimPawn.AbsOrigin ?? new Vector(0f, 0f, 0f);
            Vector AttackerPosition = AttackerPawn.AbsOrigin ?? new Vector(0f, 0f, 0f);

            float knockback_distance = GeneralPurpose.Distance(VictimPosition, AttackerPosition);

            if (knockback_distance >= 0 && weaponData.MaximumDistance >= knockback_distance)
            {
                ratio = ratio * (weaponData.MaximumDistance - knockback_distance) / 10;
            }
        }

        Vector vecDamageDirection = victimData.DamageDirection;

        vecDamageDirection = vecDamageDirection * ratio;
        vecDamageDirection.Z = VictimPawn.AbsVelocity.Z;

        VictimPawn.AbsVelocity.Add(vecDamageDirection);

        VictimDataByPlayer.Remove(Victim);

        return HookResult.Continue;
    }
}

public static class GeneralPurpose
{
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
