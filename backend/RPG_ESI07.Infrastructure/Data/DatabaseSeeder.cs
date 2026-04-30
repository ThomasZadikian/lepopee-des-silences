using RPG_ESI07.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using RPG_ESI07.Domain.Entities;
using System.Text;
using System.Text.Json;

namespace RPG_ESI07.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context, IPasswordHasher hasher)
    {
        if (await context.Users.AnyAsync())
        {
            Console.WriteLine("Database already seeded. Skipping...");
            return;
        }

        Console.WriteLine("Starting database seeding...");

        var users = new[]
        {
        new User
        {
            Username     = "devuser",
            Email        = Encoding.UTF8.GetBytes("dev@test.com"),
            PasswordHash = hasher.HashPassword("Password123!"),
            Role         = "Player",
            MfaEnabled   = false,
            CreatedAt    = DateTime.UtcNow,
            LastLoginAt  = DateTime.UtcNow.AddHours(-1),
            LastLoginIP  = "127.0.0.1",
        },
        new User
        {
            Username     = "adminuser",
            Email        = Encoding.UTF8.GetBytes("admin@test.com"),
            PasswordHash = hasher.HashPassword("AdminPass456!"),
            Role         = "Admin",
            MfaEnabled   = false,
            CreatedAt    = DateTime.UtcNow,
            LastLoginAt  = DateTime.UtcNow.AddMinutes(-30),
            LastLoginIP  = "127.0.0.1",
        },
        new User
        {
            Username     = "testplayer",
            Email        = Encoding.UTF8.GetBytes("player@test.com"),
            PasswordHash = hasher.HashPassword("TestPass789!"),
            Role         = "Player",
            MfaEnabled   = false,
            CreatedAt    = DateTime.UtcNow,
        }
    };

        // reste du seeder identique...
        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();
        Console.WriteLine($"Seeded {users.Length} users");

        // ===== 2. PLAYERPROFILES =====
        var profiles = new[]
        {
            new PlayerProfile
            {
                UserId        = users[0].Id,
                CharacterName = "Héros Dev",
                Level         = 5,
                CurrentHP     = 150,
                MaxHP         = 150,
                CurrentMP     = 80,
                MaxMP         = 80,
                Strength      = 15,
                Intelligence  = 12,
                Speed         = 14,
                Experience    = 450,
                Gold          = 250,
                UpdatedAt     = DateTime.UtcNow,
            },
            new PlayerProfile
            {
                UserId        = users[1].Id,
                CharacterName = "Admin Knight",
                Level         = 10,
                CurrentHP     = 250,
                MaxHP         = 250,
                CurrentMP     = 100,
                MaxMP         = 100,
                Strength      = 25,
                Intelligence  = 15,
                Speed         = 18,
                Experience    = 1500,
                Gold          = 1000,
                UpdatedAt     = DateTime.UtcNow,
            },
            new PlayerProfile
            {
                UserId        = users[2].Id,
                CharacterName = "Test Warrior",
                Level         = 1,
                CurrentHP     = 100,
                MaxHP         = 100,
                CurrentMP     = 50,
                MaxMP         = 50,
                Strength      = 10,
                Intelligence  = 10,
                Speed         = 10,
                Experience    = 0,
                Gold          = 100,
                UpdatedAt     = DateTime.UtcNow,
            }
        };
        await context.PlayerProfiles.AddRangeAsync(profiles);
        await context.SaveChangesAsync();
        Console.WriteLine($"Seeded {profiles.Length} player profiles");

        // ===== 3. ENEMIES =====
        var enemies = new[]
        {
            new Enemy
            {
                Name                = "Goblin",
                Type                = "basic",
                MaxHP               = 50,
                Strength            = 10,
                Intelligence        = 5,
                Speed               = 8,
                PhysicalResistance  = 1.0f,
                MagicalResistance   = 1.0f,
                ExperienceReward    = 20,
                GoldReward          = 10,
                Description         = "Petite créature verte agressive"
            },
            new Enemy
            {
                Name                = "Orc Guerrier",
                Type                = "basic",
                MaxHP               = 80,
                Strength            = 15,
                Intelligence        = 5,
                Speed               = 6,
                PhysicalResistance  = 0.9f,
                MagicalResistance   = 1.1f,
                ExperienceReward    = 35,
                GoldReward          = 20,
                Description         = "Guerrier brutal à la peau verte"
            },
            new Enemy
            {
                Name                = "Loup Sauvage",
                Type                = "basic",
                MaxHP               = 60,
                Strength            = 12,
                Intelligence        = 4,
                Speed               = 15,
                PhysicalResistance  = 1.0f,
                MagicalResistance   = 1.2f,
                ExperienceReward    = 25,
                GoldReward          = 15,
                Description         = "Prédateur rapide et agile"
            },
            new Enemy
            {
                Name                = "Squelette",
                Type                = "basic",
                MaxHP               = 70,
                Strength            = 13,
                Intelligence        = 8,
                Speed               = 9,
                PhysicalResistance  = 0.8f,
                MagicalResistance   = 1.3f,
                ExperienceReward    = 30,
                GoldReward          = 18,
                Description         = "Mort-vivant au service des ténèbres"
            },
            new Enemy
            {
                Name                = "Mage Noir",
                Type                = "miniboss",
                MaxHP               = 150,
                Strength            = 12,
                Intelligence        = 20,
                Speed               = 10,
                PhysicalResistance  = 1.5f,
                MagicalResistance   = 0.5f,
                ExperienceReward    = 100,
                GoldReward          = 50,
                Description         = "Sorcier maîtrisant les arts obscurs"
            },
            new Enemy
            {
                Name                = "Golem de Pierre",
                Type                = "miniboss",
                MaxHP               = 200,
                Strength            = 25,
                Intelligence        = 5,
                Speed               = 5,
                PhysicalResistance  = 0.6f,
                MagicalResistance   = 1.4f,
                ExperienceReward    = 120,
                GoldReward          = 60,
                Description         = "Colosse de roche animée"
            },
            new Enemy
            {
                Name                = "Dragon Ancien",
                Type                = "boss",
                MaxHP               = 500,
                Strength            = 30,
                Intelligence        = 25,
                Speed               = 15,
                PhysicalResistance  = 0.8f,
                MagicalResistance   = 0.8f,
                ExperienceReward    = 500,
                GoldReward          = 200,
                Description         = "Dragon légendaire gardien des silences"
            },
            new Enemy
            {
                Name                = "Liche Suprême",
                Type                = "boss",
                MaxHP               = 600,
                Strength            = 20,
                Intelligence        = 35,
                Speed               = 12,
                PhysicalResistance  = 1.2f,
                MagicalResistance   = 0.6f,
                ExperienceReward    = 600,
                GoldReward          = 250,
                Description         = "Nécromancien immortel maître de la mort"
            }
        };
        await context.Enemies.AddRangeAsync(enemies);
        await context.SaveChangesAsync();
        Console.WriteLine($"Seeded {enemies.Length} enemies");

        // ===== 4. ITEMS =====
        var items = new[]
        {
            new Item { Name = "Épée en Bois",       Type = "weapon",    StatModifiers = JsonSerializer.Serialize(new { strength = 5 }),                          Price = 50,  Description = "Arme de débutant simple mais efficace" },
            new Item { Name = "Épée en Fer",         Type = "weapon",    StatModifiers = JsonSerializer.Serialize(new { strength = 10 }),                         Price = 150, Description = "Lame solide forgée en fer" },
            new Item { Name = "Épée Magique",        Type = "weapon",    StatModifiers = JsonSerializer.Serialize(new { strength = 15, intelligence = 5 }),        Price = 500, Description = "Épée enchantée amplifiant la magie" },
            new Item { Name = "Armure de Cuir",      Type = "armor",     StatModifiers = JsonSerializer.Serialize(new { maxHP = 20 }),                            Price = 100, Description = "Protection légère et flexible" },
            new Item { Name = "Armure de Mailles",   Type = "armor",     StatModifiers = JsonSerializer.Serialize(new { maxHP = 40 }),                            Price = 300, Description = "Cotte de mailles résistante" },
            new Item { Name = "Armure de Plates",    Type = "armor",     StatModifiers = JsonSerializer.Serialize(new { maxHP = 60, speed = -2 }),                 Price = 800, Description = "Armure lourde offrant protection maximale" },
            new Item { Name = "Anneau de Force",     Type = "accessory", StatModifiers = JsonSerializer.Serialize(new { strength = 5 }),                          Price = 200, Description = "Anneau augmentant la force physique" },
            new Item { Name = "Amulette d'Intelligence", Type = "accessory", StatModifiers = JsonSerializer.Serialize(new { intelligence = 5, maxMP = 10 }),      Price = 250, Description = "Amulette amplifiant les capacités mentales" },
            new Item { Name = "Bottes de Vitesse",   Type = "accessory", StatModifiers = JsonSerializer.Serialize(new { speed = 5 }),                             Price = 180, Description = "Bottes permettant de se déplacer plus vite" },
            new Item { Name = "Petite Potion HP",    Type = "consumable", Category = "potion_hp",  EffectValue = 50,  Price = 20,  Description = "Restaure 50 points de vie" },
            new Item { Name = "Potion HP Moyenne",   Type = "consumable", Category = "potion_hp",  EffectValue = 100, Price = 50,  Description = "Restaure 100 points de vie" },
            new Item { Name = "Grande Potion HP",    Type = "consumable", Category = "potion_hp",  EffectValue = 200, Price = 100, Description = "Restaure 200 points de vie" },
            new Item { Name = "Petite Potion MP",    Type = "consumable", Category = "potion_mp",  EffectValue = 25,  Price = 15,  Description = "Restaure 25 points de magie" },
            new Item { Name = "Potion MP Moyenne",   Type = "consumable", Category = "potion_mp",  EffectValue = 50,  Price = 40,  Description = "Restaure 50 points de magie" },
            new Item { Name = "Grande Potion MP",    Type = "consumable", Category = "potion_mp",  EffectValue = 100, Price = 80,  Description = "Restaure 100 points de magie" },
            new Item { Name = "Élixir Complet",      Type = "consumable", Category = "elixir",     EffectValue = 999, Price = 500, Description = "Restaure complètement HP et MP" },
        };
        await context.Items.AddRangeAsync(items);
        await context.SaveChangesAsync();
        Console.WriteLine($"Seeded {items.Length} items");

        // ===== 5. SKILLS =====
        var skills = new[]
        {
            new Skill { Name = "Boule de Feu",    MPCost = 20, BaseDamage = 40,  EffectType = "damage", ElementType = "fire",      Description = "Lance une boule de feu enflammée" },
            new Skill { Name = "Éclair",           MPCost = 15, BaseDamage = 30,  EffectType = "damage", ElementType = "lightning", Description = "Frappe l'ennemi d'un éclair foudroyant" },
            new Skill { Name = "Blizzard",         MPCost = 25, BaseDamage = 50,  EffectType = "damage", ElementType = "ice",       Description = "Tempête de glace dévastatrice" },
            new Skill { Name = "Météore",          MPCost = 40, BaseDamage = 80,  EffectType = "damage", ElementType = "fire",      Description = "Invoque une pluie de météorites" },
            new Skill { Name = "Soin",             MPCost = 10, HealAmount = 50,  EffectType = "heal",   ElementType = "neutral",   Description = "Restaure les points de vie" },
            new Skill { Name = "Soin Majeur",      MPCost = 25, HealAmount = 150, EffectType = "heal",   ElementType = "neutral",   Description = "Restaure beaucoup de points de vie" },
            new Skill { Name = "Guérison Totale",  MPCost = 50, HealAmount = 999, EffectType = "heal",   ElementType = "neutral",   Description = "Restaure tous les points de vie" },
            new Skill { Name = "Rage",             MPCost = 15,                   EffectType = "buff",   ElementType = "neutral",   Description = "Augmente temporairement la force" },
            new Skill { Name = "Hâte",             MPCost = 20,                   EffectType = "buff",   ElementType = "neutral",   Description = "Augmente temporairement la vitesse" },
        };
        await context.Skills.AddRangeAsync(skills);
        await context.SaveChangesAsync();
        Console.WriteLine($"Seeded {skills.Length} skills");

        // ===== 6. COMBATSTATS =====
        var combatStats = new[]
        {
            new CombatStats
            {
                PlayerId             = profiles[0].Id,
                TotalCombats         = 15,
                CombatsWon           = 12,
                CombatsLost          = 3,
                TotalDamageDealt     = 2450,
                TotalDamageTaken     = 980,
                TotalPlaytimeMinutes = 120
            },
            new CombatStats
            {
                PlayerId             = profiles[1].Id,
                TotalCombats         = 50,
                CombatsWon           = 45,
                CombatsLost          = 5,
                TotalDamageDealt     = 12500,
                TotalDamageTaken     = 3200,
                TotalPlaytimeMinutes = 480
            },
            new CombatStats
            {
                PlayerId             = profiles[2].Id,
                TotalCombats         = 0,
                CombatsWon           = 0,
                CombatsLost          = 0,
                TotalDamageDealt     = 0,
                TotalDamageTaken     = 0,
                TotalPlaytimeMinutes = 0
            }
        };
        await context.CombatStats.AddRangeAsync(combatStats);
        await context.SaveChangesAsync();
        Console.WriteLine($"Seeded {combatStats.Length} combat stats");

        // ===== 7. PLAYERINVENTORY =====
        var inventories = new[]
        {
            // devuser
            new PlayerInventory { PlayerId = profiles[0].Id, ItemId = items[0].Id,  Quantity = 1,  IsEquipped = true  }, // Épée en Bois
            new PlayerInventory { PlayerId = profiles[0].Id, ItemId = items[3].Id,  Quantity = 1,  IsEquipped = true  }, // Armure de Cuir
            new PlayerInventory { PlayerId = profiles[0].Id, ItemId = items[9].Id,  Quantity = 5,  IsEquipped = false }, // Petite Potion HP
            new PlayerInventory { PlayerId = profiles[0].Id, ItemId = items[12].Id, Quantity = 3,  IsEquipped = false }, // Petite Potion MP

            // adminuser
            new PlayerInventory { PlayerId = profiles[1].Id, ItemId = items[2].Id,  Quantity = 1,  IsEquipped = true  }, // Épée Magique
            new PlayerInventory { PlayerId = profiles[1].Id, ItemId = items[5].Id,  Quantity = 1,  IsEquipped = true  }, // Armure de Plates
            new PlayerInventory { PlayerId = profiles[1].Id, ItemId = items[7].Id,  Quantity = 1,  IsEquipped = true  }, // Amulette Intelligence
            new PlayerInventory { PlayerId = profiles[1].Id, ItemId = items[11].Id, Quantity = 10, IsEquipped = false }, // Grande Potion HP
            new PlayerInventory { PlayerId = profiles[1].Id, ItemId = items[14].Id, Quantity = 5,  IsEquipped = false }, // Grande Potion MP
            new PlayerInventory { PlayerId = profiles[1].Id, ItemId = items[15].Id, Quantity = 2,  IsEquipped = false }, // Élixir Complet
        };
        await context.PlayerInventory.AddRangeAsync(inventories);
        await context.SaveChangesAsync();
        Console.WriteLine($"Seeded {inventories.Length} inventory items");

        // ===== 8. PLAYERSKILLS =====
        var playerSkills = new[]
        {
            // devuser
            new PlayerSkill { PlayerId = profiles[0].Id, SkillId = skills[0].Id, UnlockedAt = DateTime.UtcNow.AddDays(-10) }, // Boule de Feu
            new PlayerSkill { PlayerId = profiles[0].Id, SkillId = skills[4].Id, UnlockedAt = DateTime.UtcNow.AddDays(-5)  }, // Soin

            // adminuser — toutes les compétences
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[0].Id, UnlockedAt = DateTime.UtcNow.AddDays(-30) }, // Boule de Feu
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[1].Id, UnlockedAt = DateTime.UtcNow.AddDays(-28) }, // Éclair
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[2].Id, UnlockedAt = DateTime.UtcNow.AddDays(-25) }, // Blizzard
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[3].Id, UnlockedAt = DateTime.UtcNow.AddDays(-20) }, // Météore
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[4].Id, UnlockedAt = DateTime.UtcNow.AddDays(-30) }, // Soin
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[5].Id, UnlockedAt = DateTime.UtcNow.AddDays(-15) }, // Soin Majeur
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[6].Id, UnlockedAt = DateTime.UtcNow.AddDays(-10) }, // Guérison Totale
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[7].Id, UnlockedAt = DateTime.UtcNow.AddDays(-20) }, // Rage
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[8].Id, UnlockedAt = DateTime.UtcNow.AddDays(-18) }, // Hâte
        };
        await context.PlayerSkills.AddRangeAsync(playerSkills);
        await context.SaveChangesAsync();
        Console.WriteLine($"Seeded {playerSkills.Length} player skills");

        // ===== 9. BESTIARYUNLOCKS =====
        var bestiaryUnlocks = new[]
        {
            // devuser
            new BestiaryUnlock { PlayerId = profiles[0].Id, EnemyId = enemies[0].Id }, // Goblin
            new BestiaryUnlock { PlayerId = profiles[0].Id, EnemyId = enemies[1].Id }, // Orc
            new BestiaryUnlock { PlayerId = profiles[0].Id, EnemyId = enemies[2].Id }, // Loup

            // adminuser
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[0].Id }, // Goblin
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[1].Id }, // Orc
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[2].Id }, // Loup
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[3].Id }, // Squelette
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[4].Id }, // Mage Noir
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[5].Id }, // Golem
        };
        await context.BestiaryUnlocks.AddRangeAsync(bestiaryUnlocks);
        await context.SaveChangesAsync();
        Console.WriteLine($"Seeded {bestiaryUnlocks.Length} bestiary unlocks");

        // ===== 10. GAMESAVES =====
        var gameSaves = new[]
        {
            // devuser — save 1
            new GameSave
            {
                PlayerId      = profiles[0].Id,
                CurrentZone   = "Tutorial",
                PositionX     = 10.5f,
                PositionY     = 25.3f,
                InventoryData = JsonSerializer.Serialize(new { slotCount = 20, usedSlots = 4 }),
                QuestFlags    = JsonSerializer.Serialize(new { tutorialCompleted = true, firstBossFight = false }),
                SavedAt       = DateTime.UtcNow.AddHours(-2),
            },
            // devuser — save 2
            new GameSave
            {
                PlayerId      = profiles[0].Id,
                CurrentZone   = "BossFinal",
                PositionX     = 45.2f,
                PositionY     = 78.9f,
                InventoryData = JsonSerializer.Serialize(new { slotCount = 20, usedSlots = 8 }),
                QuestFlags    = JsonSerializer.Serialize(new { tutorialCompleted = true, firstBossFight = true }),
                SavedAt       = DateTime.UtcNow.AddMinutes(-30),
            },
            // adminuser — save
            new GameSave
            {
                PlayerId      = profiles[1].Id,
                CurrentZone   = "BossFinal",
                PositionX     = 100.0f,
                PositionY     = 150.0f,
                InventoryData = JsonSerializer.Serialize(new { slotCount = 40, usedSlots = 30 }),
                QuestFlags    = JsonSerializer.Serialize(new { tutorialCompleted = true, firstBossFight = true, finalBossUnlocked = true }),
                SavedAt       = DateTime.UtcNow.AddMinutes(-10),
            },
        };
        await context.GameSaves.AddRangeAsync(gameSaves);
        await context.SaveChangesAsync();
        Console.WriteLine($"Seeded {gameSaves.Length} game saves");

        // ===== 11. USERCONSENTS =====
        var userConsents = new[]
        {
            new UserConsent { UserId = users[0].Id, AnalyticsConsent = true,  MarketingConsent = false },
            new UserConsent { UserId = users[1].Id, AnalyticsConsent = true,  MarketingConsent = true  },
            new UserConsent { UserId = users[2].Id, AnalyticsConsent = false, MarketingConsent = false },
        };
        await context.UserConsents.AddRangeAsync(userConsents);
        await context.SaveChangesAsync();
        Console.WriteLine($"Seeded {userConsents.Length} user consents");

        // ===== 12. AUDITLOGS =====
        var auditLogs = new[]
        {
            new AuditLog
            {
                UserId    = users[0].Id,
                EventType = "LOGIN_SUCCESS",
                EventData = JsonSerializer.Serialize(new { method = "password" }),
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla/5.0",
                Timestamp = DateTime.UtcNow.AddHours(-1)
            },
            new AuditLog
            {
                UserId    = users[1].Id,
                EventType = "LOGIN_SUCCESS",
                EventData = JsonSerializer.Serialize(new { method = "password" }),
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla/5.0",
                Timestamp = DateTime.UtcNow.AddMinutes(-30)
            },
            new AuditLog
            {
                UserId    = users[0].Id,
                EventType = "DATA_EXPORT",
                EventData = JsonSerializer.Serialize(new { format = "json", size = "2.4KB" }),
                IpAddress = "127.0.0.1",
                UserAgent = "Mozilla/5.0",
                Timestamp = DateTime.UtcNow.AddMinutes(-15)
            }
        };
        await context.AuditLogs.AddRangeAsync(auditLogs);
        await context.SaveChangesAsync();
        Console.WriteLine($"Seeded {auditLogs.Length} audit logs");

        Console.WriteLine("✅ Database seeding completed successfully!");
        Console.WriteLine($"Users créés :");
        Console.WriteLine($"  devuser    / Password123!  (Player)");
        Console.WriteLine($"  adminuser  / AdminPass456! (Admin)");
        Console.WriteLine($"  testplayer / TestPass789!  (Player, sans données de jeu)");
    }
}