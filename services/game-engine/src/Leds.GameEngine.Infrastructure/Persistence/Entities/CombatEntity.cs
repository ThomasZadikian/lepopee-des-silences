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

    public int? TacticalWidth { get; set; }
    public int? TacticalHeight { get; set; }

    /// <summary>
    /// Coin haut-gauche du champ de bataille dans le référentiel de la salle. 0/0 pour un combat
    /// enregistré avant l'introduction de l'arène locale, ou pour un qui couvre la salle entière.
    /// </summary>
    public int TacticalOriginX { get; set; }
    public int TacticalOriginY { get; set; }

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
    public int? TacticalEscapeX { get; set; }
    public int? TacticalEscapeY { get; set; }
    public string? TacticalRiskTier { get; set; }
    public string? TacticalEquippedItemsCsv { get; set; }
    public string? TacticalActivationCountsCsv { get; set; }
    public string? TacticalLastMagicCsv { get; set; }
    public string? TacticalCannotReviveCsv { get; set; }

    public RunEntity? Run { get; set; }
    public List<CombatantEntity> Combatants { get; set; } = [];
}
