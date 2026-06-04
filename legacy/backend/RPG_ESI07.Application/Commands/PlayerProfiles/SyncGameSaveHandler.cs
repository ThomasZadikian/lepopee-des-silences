using MediatR;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;

namespace RPG_ESI07.Application.Commands.PlayerProfiles;

public class SyncGameSaveHandler
    : IRequestHandler<SyncGameSaveCommand, SyncGameSaveResponse>
{
    private readonly IPlayerProfileRepository _profileRepository;
    private readonly IGameSaveRepository _gameSaveRepository;
    private readonly ICombatStatsRepository _combatStatsRepository;

    public SyncGameSaveHandler(
        IPlayerProfileRepository profileRepository,
        IGameSaveRepository gameSaveRepository,
        ICombatStatsRepository combatStatsRepository)
    {
        _profileRepository = profileRepository;
        _gameSaveRepository = gameSaveRepository;
        _combatStatsRepository = combatStatsRepository;
    }

    public async Task<SyncGameSaveResponse> Handle(
        SyncGameSaveCommand request,
        CancellationToken cancellationToken)
    {
        // 1. Récupérer le profil du joueur via son UserId
        var profiles = await _profileRepository.GetAllAsync();
        var profile = profiles.FirstOrDefault(p => p.UserId == request.RequestingUserId);
        var questFlags = string.IsNullOrEmpty(request.QuestFlags) ? "{}" : request.QuestFlags;

        if (profile == null)
            throw new KeyNotFoundException(
                "Profil joueur introuvable. Le personnage doit être créé avant de sauvegarder.");

        // 2. Mettre à jour le PlayerProfile
        profile.Level = request.Level;
        profile.CurrentHP = request.CurrentHP;
        profile.MaxHP = request.MaxHP;
        profile.CurrentMP = request.CurrentMP;
        profile.MaxMP = request.MaxMP;
        profile.Strength = request.Strength;
        profile.Intelligence = request.Intelligence;
        profile.Speed = request.Speed;
        profile.Experience = request.Experience;
        profile.Gold = request.Gold;
        profile.UpdatedAt = DateTime.UtcNow;

        await _profileRepository.UpdateAsync(profile);

        // 3. Créer une nouvelle GameSave (snapshot de la partie)
        var gameSave = new GameSave
        {
            PlayerId = profile.Id,
            CurrentZone = request.CurrentZone,
            PositionX = request.PositionX,
            PositionY = request.PositionY,
            QuestFlags = questFlags,
            SavedAt = DateTime.UtcNow,
        };

        await _gameSaveRepository.AddAsync(gameSave);

        // 4. Mettre à jour ou créer les CombatStats
        var allStats = await _combatStatsRepository.GetAllAsync();
        var stats = allStats.FirstOrDefault(s => s.PlayerId == profile.Id);

        if (stats == null)
        {
            // Première sauvegarde — créer les stats
            stats = new CombatStats
            {
                PlayerId = profile.Id,
                TotalCombats = request.TotalCombats,
                CombatsWon = request.CombatsWon,
                CombatsLost = request.CombatsLost,
                TotalDamageDealt = request.TotalDamageDealt,
                TotalDamageTaken = request.TotalDamageTaken,
                TotalPlaytimeMinutes = request.TotalPlaytimeMinutes,
            };
            await _combatStatsRepository.AddAsync(stats);
        }
        else
        {
            // Mettre à jour les stats existantes
            stats.TotalCombats = request.TotalCombats;
            stats.CombatsWon = request.CombatsWon;
            stats.CombatsLost = request.CombatsLost;
            stats.TotalDamageDealt = request.TotalDamageDealt;
            stats.TotalDamageTaken = request.TotalDamageTaken;
            stats.TotalPlaytimeMinutes = request.TotalPlaytimeMinutes;
            await _combatStatsRepository.UpdateAsync(stats);
        }

        return new SyncGameSaveResponse(true, "Sauvegarde synchronisée avec succès");
    }
}