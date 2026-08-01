using Leds.Player.Domain.Common;

namespace Leds.Player.Domain.Players;

public sealed class PlayerProgression
{
    private PlayerProgression(
        int totalRunsStarted,
        int totalRunsCompleted,
        int totalRunsFailed,
        int totalRunsAbandoned,
        int unspentStatPoints,
        int totalStatPointsEarned,
        int palaceShardCount,
        int himLitShardCount)
    {
        TotalRunsStarted = totalRunsStarted;
        TotalRunsCompleted = totalRunsCompleted;
        TotalRunsFailed = totalRunsFailed;
        TotalRunsAbandoned = totalRunsAbandoned;
        UnspentStatPoints = unspentStatPoints;
        TotalStatPointsEarned = totalStatPointsEarned;
        PalaceShardCount = palaceShardCount;
        HimLitShardCount = himLitShardCount;
    }

    public int TotalRunsStarted { get; private set; }
    public int TotalRunsCompleted { get; private set; }
    public int TotalRunsFailed { get; private set; }
    public int TotalRunsAbandoned { get; private set; }
    public int UnspentStatPoints { get; private set; }
    public int TotalStatPointsEarned { get; private set; }

    /// <summary>
    /// "Éclats du Palais" — the player's persistent currency (John's rare offering).
    /// Pure counter for now: no spend mechanism exists yet.
    /// </summary>
    public int PalaceShardCount { get; private set; }

    /// <summary>
    /// "Éclats de Him'Lit" — a second persistent currency, earned only from combats at
    /// the Périlleux (1/enemy defeated) and Fatal (2/enemy defeated) danger tiers.
    /// Mirrors PalaceShardCount's plumbing exactly (see AwardHimLitCurrency/
    /// TrySpendHimLitCurrency) rather than generalizing into a multi-currency type, to
    /// avoid touching every existing PalaceShardCount call site.
    /// </summary>
    public int HimLitShardCount { get; private set; }

    public static PlayerProgression CreateDefault()
    {
        return new PlayerProgression(0, 0, 0, 0, 0, 0, 0, 0);
    }

    public void IncrementRunsStarted() => TotalRunsStarted++;
    public void IncrementRunsCompleted() => TotalRunsCompleted++;
    public void IncrementRunsFailed() => TotalRunsFailed++;
    public void IncrementRunsAbandoned() => TotalRunsAbandoned++;

    /// <summary>
    /// Awards permanent stat points. Generic on purpose: any future trigger
    /// (events, other victories, etc.) can call this same entry point.
    /// </summary>
    public void AwardStatPoint(int amount = 1)
    {
        if (amount <= 0)
            throw new DomainException("Stat point amount must be positive.");

        UnspentStatPoints += amount;
        TotalStatPointsEarned += amount;
    }

    /// <summary>
    /// Spends one unspent point. TotalStatPointsEarned is a permanent
    /// high-water mark and is never decremented by spending.
    /// </summary>
    public void SpendStatPoint()
    {
        if (UnspentStatPoints <= 0)
            throw new DomainException("No stat points available to spend.");

        UnspentStatPoints--;
    }

    /// <summary>
    /// Rehydrates player progression from a trusted persistence snapshot.
    /// This method must not be used to create new progression.
    /// </summary>
    public static PlayerProgression Rehydrate(
        int totalRunsStarted,
        int totalRunsCompleted,
        int totalRunsFailed,
        int totalRunsAbandoned = 0,
        int unspentStatPoints = 0,
        int totalStatPointsEarned = 0,
        int palaceShardCount = 0,
        int himLitShardCount = 0)
    {
        return new PlayerProgression(totalRunsStarted, totalRunsCompleted, totalRunsFailed, totalRunsAbandoned, unspentStatPoints, totalStatPointsEarned, palaceShardCount, himLitShardCount);
    }

    /// <summary>
    /// Awards "Éclats du Palais", the player's persistent currency (John's rare offering).
    /// Pure counter for now: no spend mechanism exists yet.
    /// </summary>
    public void AwardCurrency(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Currency amount must be positive.");

        PalaceShardCount += amount;
    }

    /// <summary>
    /// Spends "Éclats du Palais" if the player can afford it. Returns false (rather
    /// than throwing) on insufficient funds — insolvency is an expected outcome for
    /// callers like "Loi de l'Impôt du Seuil", not an exceptional one.
    /// </summary>
    public bool TrySpendCurrency(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Currency amount must be positive.");

        if (PalaceShardCount < amount)
            return false;

        PalaceShardCount -= amount;
        return true;
    }

    /// <summary>
    /// Awards "Éclats de Him'Lit". Mirrors AwardCurrency exactly.
    /// </summary>
    public void AwardHimLitCurrency(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Currency amount must be positive.");

        HimLitShardCount += amount;
    }

    /// <summary>
    /// Spends "Éclats de Him'Lit" if the player can afford it. Mirrors TrySpendCurrency
    /// exactly — returns false rather than throwing on insufficient funds.
    /// </summary>
    public bool TrySpendHimLitCurrency(int amount)
    {
        if (amount <= 0)
            throw new DomainException("Currency amount must be positive.");

        if (HimLitShardCount < amount)
            return false;

        HimLitShardCount -= amount;
        return true;
    }
}
