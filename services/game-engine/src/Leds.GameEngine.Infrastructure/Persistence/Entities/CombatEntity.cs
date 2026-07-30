namespace Leds.GameEngine.Infrastructure.Persistence.Entities;

public sealed class CombatEntity
{
    public Guid Id { get; set; }
    public Guid RunId { get; set; }
    public Guid RoomId { get; set; }
    public Guid NodeId { get; set; }
    public string Status { get; set; } = string.Empty;
    public int TurnNumber { get; set; }
    public int CurrentTick { get; set; }
    public int HitCounter { get; set; }
    public bool HitCounterDoubleDamageEnabled { get; set; }
    public bool FirstHitCriticalEnabled { get; set; }
    public bool HasFirstHitLanded { get; set; }
    public bool LowHpDamageAmplificationEnabled { get; set; }
    public int DotDurationExtensionTicks { get; set; }
    public bool DuelDamageAsymmetryEnabled { get; set; }
    public int DotMagnitudeBonus { get; set; }
    public bool HealingBlocked { get; set; }
    public bool FalaiseWindEnabled { get; set; }
    public bool PostDeathBasicAttackOnlyEnabled { get; set; }
    public bool NextActionRestrictedToBasicAttack { get; set; }
    public bool TapisPropreEnabled { get; set; }
    public bool ThirdCupHealCorruptionEnabled { get; set; }
    public bool PresentationsEnabled { get; set; }
    public bool MiroirEnabled { get; set; }
    public bool HasMirrorTriggered { get; set; }
    public string? ForgottenSkillKey { get; set; }
    public Guid? ActiveCombatantId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    /// <summary>
    /// « Atb » ou « Tactical ». Discriminant du déroulé : les colonnes <c>Tactical*</c> ne sont
    /// renseignées que pour le second, les colonnes ATB (tick, jauges) que pour le premier.
    /// </summary>
    /// <remarks>
    /// Une seule table pour les deux systèmes, parce que tout ce qui compte vraiment — les
    /// combattants et leur état — leur est commun et vit déjà dans <see cref="Combatants"/>.
    /// Une table parallèle aurait dupliqué cette relation sans rien gagner.
    /// </remarks>
    public string Kind { get; set; } = "Atb";

    public int? TacticalWidth { get; set; }
    public int? TacticalHeight { get; set; }

    /// <summary>Élévations, row-major, séparées par des virgules.</summary>
    public string? TacticalElevationCsv { get; set; }

    /// <summary>Praticabilité, row-major, « 1 »/« 0 » séparés par des virgules.</summary>
    public string? TacticalWalkableCsv { get; set; }

    /// <summary>Appartenance à la salle, même convention. Distincte de la praticabilité.</summary>
    public string? TacticalFloorCsv { get; set; }

    public int? TacticalRoundNumber { get; set; }
    public int? TacticalActiveIndex { get; set; }

    /// <summary>Identifiants dans l'ordre d'initiative, séparés par des points-virgules.</summary>
    public string? TacticalInitiativeOrderCsv { get; set; }

    /// <summary>Positions, au format « guid:x,y », séparées par des points-virgules.</summary>
    public string? TacticalPositionsCsv { get; set; }

    /// <summary>
    /// États de tour, au format « guid:déplacé,agi » (0/1), séparés par des points-virgules.
    /// </summary>
    public string? TacticalTurnStatesCsv { get; set; }

    /// <summary>Clés des compétences à usage unique déjà consommées, séparées par des points-virgules.</summary>
    public string? TacticalUsedOnceSkillKeysCsv { get; set; }
    public string? TacticalSkillCooldownsCsv { get; set; }

    public RunEntity? Run { get; set; }
    public List<CombatantEntity> Combatants { get; set; } = [];
}
