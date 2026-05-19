using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;

namespace RPG_ESI07.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // ── DbSets ────────────────────────────────────────────────────────────────
    public DbSet<User> Users => Set<User>();
    public DbSet<PlayerProfile> PlayerProfiles => Set<PlayerProfile>();
    public DbSet<GameSave> GameSaves => Set<GameSave>();
    public DbSet<Enemy> Enemies => Set<Enemy>();
    public DbSet<Npc> Npcs => Set<Npc>();
    public DbSet<NpcInteraction> NpcInteractions => Set<NpcInteraction>();
    public DbSet<BestiaryUnlock> BestiaryUnlocks => Set<BestiaryUnlock>();
    public DbSet<CombatStats> CombatStats => Set<CombatStats>();
    public DbSet<Item> Items => Set<Item>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<PlayerInventory> PlayerInventory => Set<PlayerInventory>();
    public DbSet<PlayerSkill> PlayerSkills => Set<PlayerSkill>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<UserConsent> UserConsents => Set<UserConsent>();
    public DbSet<CompanionState> CompanionStates => Set<CompanionState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ===== USER =====
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();

            entity.Property(e => e.Username)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Email)
                .IsRequired();

            entity.Property(e => e.PasswordHash)
                .HasMaxLength(255)
                .IsRequired();

            entity.Property(e => e.Role)
                .HasMaxLength(20)
                .HasDefaultValue(Constants.RolePlayer)
                .IsRequired();

            entity.Property(e => e.LastLoginIP)
                .HasMaxLength(45);

            entity.HasOne(e => e.PlayerProfile)
                .WithOne(e => e.User)
                .HasForeignKey<PlayerProfile>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.AuditLogs)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ===== PLAYERPROFILE =====
        modelBuilder.Entity<PlayerProfile>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CharacterName)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.CurrentZone)
                .HasMaxLength(100)
                .HasDefaultValue(string.Empty);

            entity.Property(e => e.ScalingFactor)
                .HasDefaultValue(1.0f);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_PlayerProfile_Level",
                    "\"Level\" >= 1 AND \"Level\" <= 99");
                t.HasCheckConstraint("CK_PlayerProfile_HP",
                    "\"CurrentHP\" >= 0 AND \"CurrentHP\" <= \"MaxHP\"");
                t.HasCheckConstraint("CK_PlayerProfile_MP",
                    "\"CurrentMP\" >= 0 AND \"CurrentMP\" <= \"MaxMP\"");
                t.HasCheckConstraint("CK_PlayerProfile_Stats",
                    "\"Strength\" > 0 AND \"Intelligence\" > 0 AND \"Speed\" > 0");
                t.HasCheckConstraint("CK_PlayerProfile_ScalingFactor",
                    "\"ScalingFactor\" >= 1.0");
            });

            entity.HasMany(e => e.GameSaves)
                .WithOne(e => e.Player)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.CombatStats)
                .WithOne(e => e.Player)
                .HasForeignKey<CombatStats>(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.BestiaryUnlocks)
                .WithOne(e => e.Player)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Inventory)
                .WithOne(e => e.Player)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.Skills)
                .WithOne(e => e.Player)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.NpcInteractions)
                .WithOne(e => e.Player)
                .HasForeignKey(e => e.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== ENEMY =====
        modelBuilder.Entity<Enemy>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.InitialState)
                .HasMaxLength(50)
                .HasDefaultValue(Constants.EnemyStateRepos);

            entity.Property(e => e.InfluenceRadius)
                .HasDefaultValue(5.0f);

            entity.Property(e => e.TransitionMatrix)
                .HasColumnType(Constants.JsonbColumnType);

            entity.Property(e => e.CombatScripts)
                .HasColumnType(Constants.JsonbColumnType);

            entity.Property(e => e.MapStates)
                .HasColumnType(Constants.JsonbColumnType);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Enemy_Type",
                    $"\"Type\" IN ('{Constants.EnemyTypeBasic}', '{Constants.EnemyTypeMiniboss}', '{Constants.EnemyTypeBoss}')");
                t.HasCheckConstraint("CK_Enemy_Stats",
                    "\"MaxHP\" > 0 AND \"Strength\" > 0");
                t.HasCheckConstraint("CK_Enemy_Resistance",
                    "\"PhysicalResistance\" BETWEEN 0.5 AND 2.0 AND \"MagicalResistance\" BETWEEN 0.5 AND 2.0");
                t.HasCheckConstraint("CK_Enemy_InfluenceRadius",
                    "\"InfluenceRadius\" > 0");
            });

            entity.HasMany(e => e.BestiaryUnlocks)
                .WithOne(e => e.Enemy)
                .HasForeignKey(e => e.EnemyId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== NPC =====
        modelBuilder.Entity<Npc>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name);

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.Zone)
                .HasMaxLength(100);

            entity.Property(e => e.InitialState)
                .HasMaxLength(50)
                .HasDefaultValue(Constants.NpcStateSerein);

            entity.Property(e => e.InfluenceRadius)
                .HasDefaultValue(5.0f);

            entity.Property(e => e.TransitionMatrix)
                .HasColumnType(Constants.JsonbColumnType);

            entity.Property(e => e.MapStates)
                .HasColumnType(Constants.JsonbColumnType);

            entity.Property(e => e.Dialogues)
                .HasColumnType(Constants.JsonbColumnType);

            entity.Property(e => e.MerchantInventory)
                .HasColumnType(Constants.JsonbColumnType);

            entity.Property(e => e.Quests)
                .HasColumnType(Constants.JsonbColumnType);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Npc_Type",
                    $"\"Type\" IN ('{Constants.NpcTypeNeutral}', '{Constants.NpcTypeMerchant}', '{Constants.NpcTypeQuest}', '{Constants.NpcTypeAlly}')");
                t.HasCheckConstraint("CK_Npc_InfluenceRadius",
                    "\"InfluenceRadius\" > 0");
            });

            entity.HasMany(e => e.Interactions)
                .WithOne(e => e.Npc)
                .HasForeignKey(e => e.NpcId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== NPC INTERACTION =====
        modelBuilder.Entity<NpcInteraction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.NpcId, e.PlayerId });
            entity.HasIndex(e => e.InteractedAt);

            entity.Property(e => e.EventType)
                .HasMaxLength(50)
                .IsRequired();

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_NpcInteraction_EventType",
                    "\"EventType\" IN ('DIALOGUE', 'TRADE', 'QUEST_START', 'QUEST_COMPLETE')");
            });
        });

        // ===== GAMESAVE =====
        modelBuilder.Entity<GameSave>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PlayerId, e.SavedAt });

            entity.Property(e => e.CurrentZone)
                .HasMaxLength(100)
                .IsRequired();

            entity.Property(e => e.InventoryData)
                .HasColumnType(Constants.JsonbColumnType);

            entity.Property(e => e.QuestFlags)
                .HasColumnType(Constants.JsonbColumnType);

            // Contrainte de zone supprimée — zones dynamiques
        });

        // ===== BESTIARYUNLOCK =====
        modelBuilder.Entity<BestiaryUnlock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PlayerId, e.EnemyId }).IsUnique();
        });

        // ===== COMBATSTATS =====
        modelBuilder.Entity<CombatStats>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.PlayerId).IsUnique();

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_CombatStats_Totals",
                    "\"TotalCombats\" = \"CombatsWon\" + \"CombatsLost\"");
                t.HasCheckConstraint("CK_CombatStats_Positive",
                    "\"CombatsWon\" >= 0 AND \"CombatsLost\" >= 0");
            });
        });

        // ===== ITEM =====
        modelBuilder.Entity<Item>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.Type)
                .HasMaxLength(20)
                .IsRequired();

            entity.Property(e => e.StatModifiers)
                .HasColumnType(Constants.JsonbColumnType);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Item_Type",
                    $"\"Type\" IN ('{Constants.ItemTypeWeapon}', '{Constants.ItemTypeArmor}', '{Constants.ItemTypeAccessory}', '{Constants.ItemTypeConsumable}')");
                t.HasCheckConstraint("CK_Item_Price",
                    "\"Price\" >= 0");
            });

            entity.HasMany(e => e.PlayerInventories)
                .WithOne(e => e.Item)
                .HasForeignKey(e => e.ItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== SKILL =====
        modelBuilder.Entity<Skill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Name).IsUnique();

            entity.Property(e => e.Name)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.EffectType)
                .HasMaxLength(20)
                .IsRequired();

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_Skill_EffectType",
                    $"\"EffectType\" IN ('{Constants.EffectDamage}', '{Constants.EffectHeal}', '{Constants.EffectBuff}', '{Constants.EffectDebuff}')");
                t.HasCheckConstraint("CK_Skill_MPCost",
                    "\"MPCost\" > 0");
            });

            entity.HasMany(e => e.PlayerSkills)
                .WithOne(e => e.Skill)
                .HasForeignKey(e => e.SkillId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ===== PLAYERINVENTORY =====
        modelBuilder.Entity<PlayerInventory>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PlayerId, e.ItemId }).IsUnique();

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_PlayerInventory_Quantity",
                    "\"Quantity\" > 0");
            });
        });

        // ===== PLAYERSKILL =====
        modelBuilder.Entity<PlayerSkill>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PlayerId, e.SkillId }).IsUnique();
        });

        // ===== AUDITLOG =====
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.Timestamp });
            entity.HasIndex(e => e.EventType);
            entity.HasIndex(e => e.Timestamp);

            entity.Property(e => e.EventType)
                .HasMaxLength(50)
                .IsRequired();

            entity.Property(e => e.EventData)
                .HasColumnType(Constants.JsonbColumnType);

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_AuditLog_EventType",
                    @"""EventType"" IN ('LOGIN_SUCCESS', 'LOGIN_FAILED', 'LOGOUT',
                    'DATA_EXPORT', 'DATA_DELETE', 'DATA_MODIFY',
                    'CHEAT_DETECTED', 'ADMIN_ACTION', 'MFA_ENABLED', 'MFA_FAILED')");
            });
        });

        // ===== COMPANION =====
        modelBuilder.Entity<CompanionState>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.CurrentState)
                .HasMaxLength(20)
                .HasDefaultValue(Constants.CompanionStateRepos);

            entity.ToTable(t =>
            {
                t.HasCheckConstraint("CK_CompanionState_State",
                    $"\"CurrentState\" IN ('{Constants.CompanionStateRepos}', '{Constants.CompanionStateJeu}', '{Constants.CompanionStateManger}', '{Constants.CompanionStateExcite}', '{Constants.CompanionStateTriste}', '{Constants.CompanionStateEndormi}')");
            });
        });

        // ===== USERCONSENT =====
        modelBuilder.Entity<UserConsent>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}