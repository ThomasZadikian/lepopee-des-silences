using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using System.Text;
using System.Text.Json;

namespace RPG_ESI07.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context, IPasswordHasher hasher, IConfiguration? configuration = null)
    {
        if (await context.Users.AnyAsync())
        {
            return;
        }

        var playerPassword = GetRequiredSeedPassword(configuration, "Seed:PlayerPassword");
        var adminPassword = GetRequiredSeedPassword(configuration, "Seed:AdminPassword");
        var testPassword = GetRequiredSeedPassword(configuration, "Seed:TestPassword");

        // ===== 1. USERS =====
        var users = new[]
        {
            new User
            {
                Username    = "devuser",
                Email       = Encoding.UTF8.GetBytes("dev@rpg-esi07.com"),
                PasswordHash= hasher.HashPassword(playerPassword),
                Role        = "Player",
                MfaEnabled  = false,
                CreatedAt   = DateTime.UtcNow,
                LastLoginAt = DateTime.UtcNow.AddHours(-1),
                LastLoginIP = "127.0.0.1",
            },
            new User
            {
                Username    = "adminuser",
                Email       = Encoding.UTF8.GetBytes("admin@rpg-esi07.com"),
                PasswordHash= hasher.HashPassword(adminPassword),
                Role        = "Admin",
                MfaEnabled  = false,
                CreatedAt   = DateTime.UtcNow.AddDays(-60),
                LastLoginAt = DateTime.UtcNow.AddMinutes(-30),
                LastLoginIP = "127.0.0.1",
            },
            new User
            {
                Username    = "testplayer",
                Email       = Encoding.UTF8.GetBytes("player@rpg-esi07.com"),
                PasswordHash= hasher.HashPassword(testPassword),
                Role        = "Player",
                MfaEnabled  = false,
                CreatedAt   = DateTime.UtcNow.AddDays(-10),
                LastLoginAt = DateTime.UtcNow.AddDays(-2),
                LastLoginIP = "127.0.0.1",
            }
        };
        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        var profiles = new[]
        {
            new PlayerProfile
            {
                UserId        = users[0].Id,
                CharacterName = "Elara",
                Level         = 8,
                CurrentHP     = 120,
                MaxHP         = 140,
                CurrentMP     = 200,
                MaxMP         = 200,
                Strength      = 8,
                Intelligence  = 28,
                Speed         = 14,
                Experience    = 720,
                Gold          = 580,
                UpdatedAt     = DateTime.UtcNow.AddHours(-1),
            },
            new PlayerProfile
            {
                UserId        = users[1].Id,
                CharacterName = "Kael le Briseur",
                Level         = 25,
                CurrentHP     = 850,
                MaxHP         = 850,
                CurrentMP     = 80,
                MaxMP         = 80,
                Strength      = 45,
                Intelligence  = 10,
                Speed         = 12,
                Experience    = 600,
                Gold          = 12500,
                UpdatedAt     = DateTime.UtcNow.AddMinutes(-30),
            },
            new PlayerProfile
            {
                UserId        = users[2].Id,
                CharacterName = "Zyx",
                Level         = 3,
                CurrentHP     = 75,
                MaxHP         = 90,
                CurrentMP     = 40,
                MaxMP         = 60,
                Strength      = 12,
                Intelligence  = 10,
                Speed         = 22,
                Experience    = 180,
                Gold          = 95,
                UpdatedAt     = DateTime.UtcNow.AddDays(-2),
            }
        };
        await context.PlayerProfiles.AddRangeAsync(profiles);
        await context.SaveChangesAsync();

        // ===== 3. ENEMIES (20 ennemis) =====
        var enemies = new[]
        {
            // ── Basiques ──────────────────────────────────────────────────────
            new Enemy
            {
                Name = "Gobelin des Bois", Type = "basic", MaxHP = 45, Strength = 8,
                Intelligence = 4, Speed = 10, PhysicalResistance = 1.0f, MagicalResistance = 1.2f,
                ExperienceReward = 15, GoldReward = 8,
                Description = "Petite créature rusée qui tend des embuscades dans les forêts denses"
            },
            new Enemy
            {
                Name = "Rat Géant", Type = "basic", MaxHP = 30, Strength = 6,
                Intelligence = 2, Speed = 14, PhysicalResistance = 1.0f, MagicalResistance = 1.0f,
                ExperienceReward = 10, GoldReward = 3,
                Description = "Rongeur mutant dont les dents percent l'armure de cuir"
            },
            new Enemy
            {
                Name = "Orc Guerrier", Type = "basic", MaxHP = 85, Strength = 16,
                Intelligence = 5, Speed = 7, PhysicalResistance = 0.9f, MagicalResistance = 1.1f,
                ExperienceReward = 35, GoldReward = 20,
                Description = "Guerrier brutal dont la rage compense le manque de finesse"
            },
            new Enemy
            {
                Name = "Loup des Cendres", Type = "basic", MaxHP = 65, Strength = 13,
                Intelligence = 5, Speed = 18, PhysicalResistance = 1.0f, MagicalResistance = 1.0f,
                ExperienceReward = 28, GoldReward = 12,
                Description = "Prédateur nocturne au pelage noirci par la magie corrompue"
            },
            new Enemy
            {
                Name = "Squelette Archer", Type = "basic", MaxHP = 55, Strength = 10,
                Intelligence = 6, Speed = 9, PhysicalResistance = 0.8f, MagicalResistance = 1.4f,
                ExperienceReward = 25, GoldReward = 15,
                Description = "Mort-vivant dont les os craquent à chaque tir, mais qui ne manque jamais"
            },
            new Enemy
            {
                Name = "Araignée Venimeuse", Type = "basic", MaxHP = 50, Strength = 9,
                Intelligence = 3, Speed = 16, PhysicalResistance = 1.1f, MagicalResistance = 0.9f,
                ExperienceReward = 22, GoldReward = 10,
                Description = "Son venin paralyse ses proies avant qu'elle les cocon"
            },
            new Enemy
            {
                Name = "Bandit de Grand Chemin", Type = "basic", MaxHP = 70, Strength = 12,
                Intelligence = 8, Speed = 11, PhysicalResistance = 1.0f, MagicalResistance = 1.0f,
                ExperienceReward = 30, GoldReward = 25,
                Description = "Ancien soldat reconverti dans le pillage après la guerre"
            },
            new Enemy
            {
                Name = "Zombie Pesteux", Type = "basic", MaxHP = 90, Strength = 14,
                Intelligence = 1, Speed = 4, PhysicalResistance = 0.7f, MagicalResistance = 1.3f,
                ExperienceReward = 32, GoldReward = 5,
                Description = "Sa lenteur cache une résistance aux dommages physiques déconcertante"
            },
            new Enemy
            {
                Name = "Imp Pyromaniac", Type = "basic", MaxHP = 40, Strength = 7,
                Intelligence = 15, Speed = 13, PhysicalResistance = 1.2f, MagicalResistance = 0.6f,
                ExperienceReward = 28, GoldReward = 18,
                Description = "Petit démon obsédé par les flammes, incapable de résister à y jouer"
            },
            new Enemy
            {
                Name = "Troll des Marais", Type = "basic", MaxHP = 120, Strength = 18,
                Intelligence = 3, Speed = 5, PhysicalResistance = 0.8f, MagicalResistance = 1.2f,
                ExperienceReward = 45, GoldReward = 22,
                Description = "Sa régénération passive en fait un adversaire épuisant à l'usure"
            },
            // ── Miniboss ──────────────────────────────────────────────────────
            new Enemy
            {
                Name = "Mage Corrompu", Type = "miniboss", MaxHP = 180, Strength = 10,
                Intelligence = 30, Speed = 8, PhysicalResistance = 1.6f, MagicalResistance = 0.5f,
                ExperienceReward = 120, GoldReward = 65,
                Description = "Érudit ayant vendu son âme aux ténèbres pour un pouvoir illimité"
            },
            new Enemy
            {
                Name = "Golem de Fer", Type = "miniboss", MaxHP = 250, Strength = 28,
                Intelligence = 2, Speed = 4, PhysicalResistance = 0.5f, MagicalResistance = 1.6f,
                ExperienceReward = 140, GoldReward = 75,
                Description = "Automate de guerre oublié dont la conscience s'est éteinte depuis longtemps"
            },
            new Enemy
            {
                Name = "Vampire Ancien", Type = "miniboss", MaxHP = 200, Strength = 22,
                Intelligence = 25, Speed = 20, PhysicalResistance = 1.3f, MagicalResistance = 0.7f,
                ExperienceReward = 160, GoldReward = 90,
                Description = "Seigneur de la nuit dont chaque coup vole la vitalité de ses victimes"
            },
            new Enemy
            {
                Name = "Chevalier Maudit", Type = "miniboss", MaxHP = 220, Strength = 30,
                Intelligence = 12, Speed = 10, PhysicalResistance = 0.5f, MagicalResistance = 1.5f,
                ExperienceReward = 150, GoldReward = 80,
                Description = "Paladin déchu lié à son armure pour l'éternité par une malédiction ancienne"
            },
            new Enemy
            {
                Name = "Wyverne des Pics", Type = "miniboss", MaxHP = 300, Strength = 25,
                Intelligence = 8, Speed = 22, PhysicalResistance = 0.9f, MagicalResistance = 1.1f,
                ExperienceReward = 180, GoldReward = 100,
                Description = "Dragon immature dont le venin acide dévore l'acier comme du tissu"
            },
            // ── Boss ──────────────────────────────────────────────────────────
            new Enemy
            {
                Name = "Archiliches Tenebris", Type = "boss", MaxHP = 700, Strength = 20,
                Intelligence = 50, Speed = 10, PhysicalResistance = 1.5f, MagicalResistance = 0.5f,
                ExperienceReward = 800, GoldReward = 350,
                Description = "Nécromancien ayant transcendé la mort, tissant la réalité à sa volonté"
            },
            new Enemy
            {
                Name = "Dragon de l'Aube", Type = "boss", MaxHP = 900, Strength = 40,
                Intelligence = 30, Speed = 20, PhysicalResistance = 0.7f, MagicalResistance = 0.7f,
                ExperienceReward = 1000, GoldReward = 500,
                Description = "Dernier représentant d'une race de dragons solaires, gardien du sanctuaire"
            },
            new Enemy
            {
                Name = "Le Dévoreur", Type = "boss", MaxHP = 1200, Strength = 55,
                Intelligence = 15, Speed = 8, PhysicalResistance = 0.6f, MagicalResistance = 0.6f,
                ExperienceReward = 1200, GoldReward = 400,
                Description = "Entité primordiale sans forme définie, absorbant toute énergie vitale"
            },
            new Enemy
            {
                Name = "Seigneur des Ombres", Type = "boss", MaxHP = 800, Strength = 35,
                Intelligence = 40, Speed = 25, PhysicalResistance = 1.0f, MagicalResistance = 0.5f,
                ExperienceReward = 950, GoldReward = 450,
                Description = "Maître de l'illusion et de l'assassinat, se battant dans plusieurs dimensions"
            },
            new Enemy
            {
                Name = "Titan de Pierre", Type = "boss", MaxHP = 1500, Strength = 60,
                Intelligence = 5, Speed = 3, PhysicalResistance = 0.5f, MagicalResistance = 1.7f,
                ExperienceReward = 1500, GoldReward = 600,
                Description = "Colosse millénaire réveillé d'un sommeil de pierre, dont chaque pas ébranle la terre"
            },
        };
        await context.Enemies.AddRangeAsync(enemies);
        await context.SaveChangesAsync();

        // ===== 4. ITEMS (30 items) =====
        var items = new[]
        {
            // ── Armes ─────────────────────────────────────────────────────────
            new Item { Name = "Dague Rouillée",       Type = "weapon", StatModifiers = JsonSerializer.Serialize(new { strength = 3 }),                           Price = 20,   Description = "Arme de fortune aux bords ébréchés, mais mortelle entre de bonnes mains" },
            new Item { Name = "Épée Courte",           Type = "weapon", StatModifiers = JsonSerializer.Serialize(new { strength = 7 }),                           Price = 80,   Description = "Lame équilibrée pour les combattants débutants" },
            new Item { Name = "Épée Longue en Fer",    Type = "weapon", StatModifiers = JsonSerializer.Serialize(new { strength = 12 }),                          Price = 200,  Description = "Arme à deux mains forgée par les forgerons du Nord" },
            new Item { Name = "Lame de Lumière",       Type = "weapon", StatModifiers = JsonSerializer.Serialize(new { strength = 18, intelligence = 6 }),        Price = 650,  Description = "Épée bénie capable de dissoudre les non-morts au contact" },
            new Item { Name = "Bâton des Arcanes",     Type = "weapon", StatModifiers = JsonSerializer.Serialize(new { intelligence = 20, maxMP = 40 }),          Price = 750,  Description = "Focalisateur magique taillé dans du bois de sorbier lunaire" },
            new Item { Name = "Grimoire du Vide",      Type = "weapon", StatModifiers = JsonSerializer.Serialize(new { intelligence = 30, maxMP = 60 }),          Price = 1200, Description = "Tome interdit contenant des sorts oubliés depuis des siècles" },
            new Item { Name = "Arc Elfique",           Type = "weapon", StatModifiers = JsonSerializer.Serialize(new { strength = 10, speed = 5 }),               Price = 420,  Description = "Arc en if enchanté dont les flèches semblent chercher leur cible" },
            new Item { Name = "Hache de Guerre",       Type = "weapon", StatModifiers = JsonSerializer.Serialize(new { strength = 22, speed = -3 }),              Price = 480,  Description = "Arme brutale qui sacrifie la mobilité pour une puissance dévastatrice" },
            new Item { Name = "Poignards Jumeaux",     Type = "weapon", StatModifiers = JsonSerializer.Serialize(new { strength = 8, speed = 8 }),                Price = 380,  Description = "Paire de lames conçue pour les attaques rapides en succession" },
            new Item { Name = "Épée Runique",          Type = "weapon", StatModifiers = JsonSerializer.Serialize(new { strength = 25, intelligence = 10 }),       Price = 1500, Description = "Lame gravée de runes ancestrales canalisent la magie vers le fil" },

            // ── Armures ───────────────────────────────────────────────────────
            new Item { Name = "Vêtements en Cuir",     Type = "armor",  StatModifiers = JsonSerializer.Serialize(new { maxHP = 15 }),                            Price = 60,   Description = "Protection minimale mais légère, idéale pour les voleurs" },
            new Item { Name = "Tunique de Mage",       Type = "armor",  StatModifiers = JsonSerializer.Serialize(new { maxHP = 10, maxMP = 20 }),                Price = 120,  Description = "Tissu enchanté qui amplifie les capacités magiques" },
            new Item { Name = "Cotte de Mailles",      Type = "armor",  StatModifiers = JsonSerializer.Serialize(new { maxHP = 45 }),                            Price = 350,  Description = "Protection intermédiaire offrant mobilité et résistance" },
            new Item { Name = "Plastron d'Acier",      Type = "armor",  StatModifiers = JsonSerializer.Serialize(new { maxHP = 70, speed = -1 }),                Price = 600,  Description = "Armure lourde forgée pour les guerriers de première ligne" },
            new Item { Name = "Armure du Crépuscule",  Type = "armor",  StatModifiers = JsonSerializer.Serialize(new { maxHP = 90, maxMP = 30 }),                Price = 1100, Description = "Alliage rare absorbant à la fois les coups physiques et magiques" },

            // ── Accessoires ───────────────────────────────────────────────────
            new Item { Name = "Anneau de Force",       Type = "accessory", StatModifiers = JsonSerializer.Serialize(new { strength = 5 }),                       Price = 180,  Description = "Anneau forgé avec du métal des profondeurs, augmentant la puissance" },
            new Item { Name = "Amulette d'Esprit",     Type = "accessory", StatModifiers = JsonSerializer.Serialize(new { intelligence = 8, maxMP = 15 }),       Price = 300,  Description = "Pendentif en cristal affinant la perception magique" },
            new Item { Name = "Bottes de Mercure",     Type = "accessory", StatModifiers = JsonSerializer.Serialize(new { speed = 8 }),                          Price = 250,  Description = "Chaussures enchantées par un esprit du vent, presque trop rapides" },
            new Item { Name = "Ceinture du Survivant", Type = "accessory", StatModifiers = JsonSerializer.Serialize(new { maxHP = 30 }),                         Price = 220,  Description = "Accessoire tressé avec des fibres régénératrices" },
            new Item { Name = "Bague du Savant",       Type = "accessory", StatModifiers = JsonSerializer.Serialize(new { intelligence = 12, maxMP = 25 }),      Price = 550,  Description = "Anneau d'un mage défunt, encore imprégné de sa puissance" },
            new Item { Name = "Cape de l'Ombre",       Type = "accessory", StatModifiers = JsonSerializer.Serialize(new { speed = 6, strength = 4 }),            Price = 480,  Description = "Cape tissée avec des fils d'obscurité, rend son porteur plus difficile à percevoir" },

            // ── Consommables HP ───────────────────────────────────────────────
            new Item { Name = "Herbe Cicatrisante",    Type = "consumable", Category = "potion_hp",  EffectValue = 30,  Price = 12,  Description = "Plante médicinale commune stopant les saignements" },
            new Item { Name = "Fiole de Soin",         Type = "consumable", Category = "potion_hp",  EffectValue = 60,  Price = 28,  Description = "Décoction alchimique réparant les tissus endommagés" },
            new Item { Name = "Potion de Vitalité",    Type = "consumable", Category = "potion_hp",  EffectValue = 120, Price = 60,  Description = "Formule concentrée restaurant une large part des forces" },
            new Item { Name = "Élixir de Guérison",    Type = "consumable", Category = "potion_hp",  EffectValue = 250, Price = 150, Description = "Préparation rare close toutes les blessures en instants" },

            // ── Consommables MP ───────────────────────────────────────────────
            new Item { Name = "Cristal de Mana",       Type = "consumable", Category = "potion_mp",  EffectValue = 30,  Price = 15,  Description = "Fragment cristallin libérant de l'énergie magique pure" },
            new Item { Name = "Essence Magique",       Type = "consumable", Category = "potion_mp",  EffectValue = 70,  Price = 40,  Description = "Liquide luminescent distillé d'éthers magiques" },
            new Item { Name = "Potion d'Éther",        Type = "consumable", Category = "potion_mp",  EffectValue = 150, Price = 90,  Description = "Potion rare restaurant une grande quantité de mana" },

            // ── Élixirs ───────────────────────────────────────────────────────
            new Item { Name = "Élixir de Puissance",   Type = "consumable", Category = "elixir",     EffectValue = 500, Price = 350, Description = "Double temporairement la force et l'intelligence du buveur" },
            new Item { Name = "Panacée Légendaire",    Type = "consumable", Category = "elixir",     EffectValue = 999, Price = 800, Description = "Remède universel restaurant complètement HP et MP, quasi introuvable" },
        };
        await context.Items.AddRangeAsync(items);
        await context.SaveChangesAsync();

        // ===== 5. SKILLS (18 compétences) =====
        var skills = new[]
        {
            // ── Attaque ───────────────────────────────────────────────────────
            new Skill { Name = "Boule de Feu",      MPCost = 15, BaseDamage = 35,  EffectType = "damage", ElementType = "fire",      Description = "Projectile enflammé infligeant des dégâts modérés de feu" },
            new Skill { Name = "Foudre",             MPCost = 18, BaseDamage = 45,  EffectType = "damage", ElementType = "lightning", Description = "Éclair direct frappant avec une précision absolue" },
            new Skill { Name = "Blizzard",           MPCost = 30, BaseDamage = 60,  EffectType = "damage", ElementType = "ice",       Description = "Tempête glaciale ralentissant et blessant les ennemis" },
            new Skill { Name = "Météore",            MPCost = 45, BaseDamage = 95,  EffectType = "damage", ElementType = "fire",      Description = "Rocher céleste embrasé s'abattant avec une force dévastatrice" },
            new Skill { Name = "Lance Sacrée",       MPCost = 25, BaseDamage = 55,  EffectType = "damage", ElementType = "neutral",   Description = "Projectile de lumière pure particulièrement efficace contre les morts-vivants" },
            new Skill { Name = "Lame du Vent",       MPCost = 12, BaseDamage = 28,  EffectType = "damage", ElementType = "neutral",   Description = "Tranchant invisible propulsé à grande vitesse" },
            new Skill { Name = "Nova de Ténèbres",   MPCost = 55, BaseDamage = 110, EffectType = "damage", ElementType = "neutral",   Description = "Explosion d'énergie sombre ravageant tout ce qu'elle touche" },

            // ── Soin ──────────────────────────────────────────────────────────
            new Skill { Name = "Soin",               MPCost = 10, HealAmount = 50,   EffectType = "heal",   ElementType = "neutral", Description = "Incantation basique restaurant une partie des points de vie" },
            new Skill { Name = "Soin Mineur",        MPCost = 6,  HealAmount = 25,   EffectType = "heal",   ElementType = "neutral", Description = "Formule rapide pour les petites blessures" },
            new Skill { Name = "Régénération",       MPCost = 20, HealAmount = 80,   EffectType = "heal",   ElementType = "neutral", Description = "Accélère la cicatrisation sur plusieurs secondes" },
            new Skill { Name = "Soin de Groupe",     MPCost = 35, HealAmount = 60,   EffectType = "heal",   ElementType = "neutral", Description = "Aura curative soignant tous les alliés proches simultanément" },
            new Skill { Name = "Résurrection",       MPCost = 80, HealAmount = 200,  EffectType = "heal",   ElementType = "neutral", Description = "Sort ultime permettant de rappeler un allié à la vie" },

            // ── Buff ──────────────────────────────────────────────────────────
            new Skill { Name = "Rage Berserker",     MPCost = 20, EffectType = "buff",   ElementType = "neutral", Description = "Décuple la force physique au prix de la défense" },
            new Skill { Name = "Hâte",               MPCost = 15, EffectType = "buff",   ElementType = "neutral", Description = "Accélère les mouvements et les attaques pendant quelques instants" },
            new Skill { Name = "Bouclier Magique",   MPCost = 25, EffectType = "buff",   ElementType = "neutral", Description = "Barrière énergétique absorbant les prochains dégâts magiques" },
            new Skill { Name = "Bénédiction",        MPCost = 30, EffectType = "buff",   ElementType = "neutral", Description = "Invocation divine augmentant toutes les statistiques temporairement" },

            // ── Debuff ────────────────────────────────────────────────────────
            new Skill { Name = "Malédiction",        MPCost = 22, EffectType = "debuff",  ElementType = "neutral", Description = "Réduit la résistance de l'ennemi à toutes les formes de dégâts" },
            new Skill { Name = "Ralentissement",     MPCost = 14, EffectType = "debuff",  ElementType = "ice",     Description = "Congèle partiellement les membres de la cible, réduisant sa vitesse" },
        };
        await context.Skills.AddRangeAsync(skills);
        await context.SaveChangesAsync();

        // ===== 6. COMBATSTATS =====
        var combatStats = new[]
        {
            new CombatStats
            {
                PlayerId             = profiles[0].Id,
                TotalCombats         = 42,
                CombatsWon           = 35,
                CombatsLost          = 7,
                TotalDamageDealt     = 18500,
                TotalDamageTaken     = 4200,
                TotalPlaytimeMinutes = 280
            },
            new CombatStats
            {
                PlayerId             = profiles[1].Id,
                TotalCombats         = 318,
                CombatsWon           = 298,
                CombatsLost          = 20,
                TotalDamageDealt     = 145000,
                TotalDamageTaken     = 62000,
                TotalPlaytimeMinutes = 2880
            },
            new CombatStats
            {
                PlayerId             = profiles[2].Id,
                TotalCombats         = 8,
                CombatsWon           = 5,
                CombatsLost          = 3,
                TotalDamageDealt     = 950,
                TotalDamageTaken     = 780,
                TotalPlaytimeMinutes = 45
            }
        };
        await context.CombatStats.AddRangeAsync(combatStats);
        await context.SaveChangesAsync();

        // ===== 7. PLAYERINVENTORY =====
        var inventories = new[]
        {
            // Elara (mage) — focalisée sur la magie
            new PlayerInventory { PlayerId = profiles[0].Id, ItemId = items[4].Id,  Quantity = 1,  IsEquipped = true  }, // Bâton des Arcanes
            new PlayerInventory { PlayerId = profiles[0].Id, ItemId = items[11].Id, Quantity = 1,  IsEquipped = true  }, // Tunique de Mage
            new PlayerInventory { PlayerId = profiles[0].Id, ItemId = items[16].Id, Quantity = 1,  IsEquipped = true  }, // Amulette d'Esprit
            new PlayerInventory { PlayerId = profiles[0].Id, ItemId = items[21].Id, Quantity = 4,  IsEquipped = false }, // Herbe Cicatrisante
            new PlayerInventory { PlayerId = profiles[0].Id, ItemId = items[25].Id, Quantity = 6,  IsEquipped = false }, // Cristal de Mana
            new PlayerInventory { PlayerId = profiles[0].Id, ItemId = items[22].Id, Quantity = 2,  IsEquipped = false }, // Fiole de Soin
            new PlayerInventory { PlayerId = profiles[0].Id, ItemId = items[26].Id, Quantity = 3,  IsEquipped = false }, // Essence Magique

            // Kael (guerrier tank) — axé résistance et force
            new PlayerInventory { PlayerId = profiles[1].Id, ItemId = items[9].Id,  Quantity = 1,  IsEquipped = true  }, // Épée Runique
            new PlayerInventory { PlayerId = profiles[1].Id, ItemId = items[14].Id, Quantity = 1,  IsEquipped = true  }, // Armure du Crépuscule
            new PlayerInventory { PlayerId = profiles[1].Id, ItemId = items[15].Id, Quantity = 1,  IsEquipped = true  }, // Anneau de Force
            new PlayerInventory { PlayerId = profiles[1].Id, ItemId = items[18].Id, Quantity = 1,  IsEquipped = true  }, // Ceinture du Survivant
            new PlayerInventory { PlayerId = profiles[1].Id, ItemId = items[20].Id, Quantity = 1,  IsEquipped = true  }, // Cape de l'Ombre
            new PlayerInventory { PlayerId = profiles[1].Id, ItemId = items[23].Id, Quantity = 10, IsEquipped = false }, // Potion de Vitalité
            new PlayerInventory { PlayerId = profiles[1].Id, ItemId = items[24].Id, Quantity = 5,  IsEquipped = false }, // Élixir de Guérison
            new PlayerInventory { PlayerId = profiles[1].Id, ItemId = items[27].Id, Quantity = 3,  IsEquipped = false }, // Potion d'Éther
            new PlayerInventory { PlayerId = profiles[1].Id, ItemId = items[28].Id, Quantity = 2,  IsEquipped = false }, // Élixir de Puissance
            new PlayerInventory { PlayerId = profiles[1].Id, ItemId = items[29].Id, Quantity = 1,  IsEquipped = false }, // Panacée Légendaire

            // Zyx (voleur débutant) — léger, rapide
            new PlayerInventory { PlayerId = profiles[2].Id, ItemId = items[0].Id,  Quantity = 1,  IsEquipped = true  }, // Dague Rouillée
            new PlayerInventory { PlayerId = profiles[2].Id, ItemId = items[10].Id, Quantity = 1,  IsEquipped = true  }, // Vêtements en Cuir
            new PlayerInventory { PlayerId = profiles[2].Id, ItemId = items[21].Id, Quantity = 2,  IsEquipped = false }, // Herbe Cicatrisante
        };
        await context.PlayerInventory.AddRangeAsync(inventories);
        await context.SaveChangesAsync();

        // ===== 8. PLAYERSKILLS =====
        var playerSkills = new[]
        {
            // Elara (mage) — sorts offensifs et soins
            new PlayerSkill { PlayerId = profiles[0].Id, SkillId = skills[0].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-20) }, // Boule de Feu
            new PlayerSkill { PlayerId = profiles[0].Id, SkillId = skills[1].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-15) }, // Foudre
            new PlayerSkill { PlayerId = profiles[0].Id, SkillId = skills[7].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-18) }, // Soin
            new PlayerSkill { PlayerId = profiles[0].Id, SkillId = skills[8].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-25) }, // Soin Mineur
            new PlayerSkill { PlayerId = profiles[0].Id, SkillId = skills[13].Id, UnlockedAt = DateTime.UtcNow.AddDays(-10) }, // Hâte
            new PlayerSkill { PlayerId = profiles[0].Id, SkillId = skills[16].Id, UnlockedAt = DateTime.UtcNow.AddDays(-8)  }, // Malédiction

            // Kael (guerrier) — buffs et attaques physiques
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[0].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-90) }, // Boule de Feu
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[2].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-80) }, // Blizzard
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[3].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-70) }, // Météore
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[4].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-60) }, // Lance Sacrée
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[6].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-50) }, // Nova de Ténèbres
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[7].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-85) }, // Soin
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[9].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-75) }, // Régénération
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[11].Id, UnlockedAt = DateTime.UtcNow.AddDays(-40) }, // Résurrection
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[12].Id, UnlockedAt = DateTime.UtcNow.AddDays(-65) }, // Rage Berserker
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[13].Id, UnlockedAt = DateTime.UtcNow.AddDays(-55) }, // Hâte
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[14].Id, UnlockedAt = DateTime.UtcNow.AddDays(-45) }, // Bouclier Magique
            new PlayerSkill { PlayerId = profiles[1].Id, SkillId = skills[15].Id, UnlockedAt = DateTime.UtcNow.AddDays(-35) }, // Bénédiction

            // Zyx (débutant) — une seule compétence
            new PlayerSkill { PlayerId = profiles[2].Id, SkillId = skills[5].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-3) },  // Lame du Vent
        };
        await context.PlayerSkills.AddRangeAsync(playerSkills);
        await context.SaveChangesAsync();

        // ===== 9. BESTIARYUNLOCKS =====
        var bestiaryUnlocks = new[]
        {
            // Elara — a rencontré des ennemis de début et milieu de jeu
            new BestiaryUnlock { PlayerId = profiles[0].Id, EnemyId = enemies[0].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-25) }, // Gobelin
            new BestiaryUnlock { PlayerId = profiles[0].Id, EnemyId = enemies[1].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-22) }, // Rat Géant
            new BestiaryUnlock { PlayerId = profiles[0].Id, EnemyId = enemies[2].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-18) }, // Orc
            new BestiaryUnlock { PlayerId = profiles[0].Id, EnemyId = enemies[3].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-15) }, // Loup
            new BestiaryUnlock { PlayerId = profiles[0].Id, EnemyId = enemies[5].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-12) }, // Araignée
            new BestiaryUnlock { PlayerId = profiles[0].Id, EnemyId = enemies[8].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-8)  }, // Imp
            new BestiaryUnlock { PlayerId = profiles[0].Id, EnemyId = enemies[10].Id, UnlockedAt = DateTime.UtcNow.AddDays(-5)  }, // Mage Corrompu

            // Kael — vétéran, a tout rencontré
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[0].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-180) },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[1].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-175) },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[2].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-170) },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[3].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-165) },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[4].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-160) },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[5].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-155) },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[6].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-150) },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[7].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-140) },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[8].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-130) },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[9].Id,  UnlockedAt = DateTime.UtcNow.AddDays(-120) },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[10].Id, UnlockedAt = DateTime.UtcNow.AddDays(-100) },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[11].Id, UnlockedAt = DateTime.UtcNow.AddDays(-90)  },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[12].Id, UnlockedAt = DateTime.UtcNow.AddDays(-80)  },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[13].Id, UnlockedAt = DateTime.UtcNow.AddDays(-70)  },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[14].Id, UnlockedAt = DateTime.UtcNow.AddDays(-60)  },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[15].Id, UnlockedAt = DateTime.UtcNow.AddDays(-50)  },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[16].Id, UnlockedAt = DateTime.UtcNow.AddDays(-40)  },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[17].Id, UnlockedAt = DateTime.UtcNow.AddDays(-30)  },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[18].Id, UnlockedAt = DateTime.UtcNow.AddDays(-20)  },
            new BestiaryUnlock { PlayerId = profiles[1].Id, EnemyId = enemies[19].Id, UnlockedAt = DateTime.UtcNow.AddDays(-10)  },

            // Zyx — débutant, a rencontré quelques ennemis
            new BestiaryUnlock { PlayerId = profiles[2].Id, EnemyId = enemies[0].Id, UnlockedAt = DateTime.UtcNow.AddDays(-8) }, // Gobelin
            new BestiaryUnlock { PlayerId = profiles[2].Id, EnemyId = enemies[1].Id, UnlockedAt = DateTime.UtcNow.AddDays(-5) }, // Rat Géant
        };
        await context.BestiaryUnlocks.AddRangeAsync(bestiaryUnlocks);
        await context.SaveChangesAsync();

        // ===== 10. GAMESAVES =====
        var gameSaves = new[]
        {
            // Elara — 2 saves
            new GameSave
            {
                PlayerId      = profiles[0].Id,
                CurrentZone   = "Tutorial",
                PositionX     = 15.2f, PositionY = 32.8f,
                InventoryData = JsonSerializer.Serialize(new { slotCount = 20, usedSlots = 7 }),
                QuestFlags    = JsonSerializer.Serialize(new { tutorialCompleted = true, firstBossFight = false }),
                SavedAt       = DateTime.UtcNow.AddHours(-3),
            },
            new GameSave
            {
                PlayerId      = profiles[0].Id,
                CurrentZone   = "BossFinal",
                PositionX     = 88.4f, PositionY = 42.1f,
                InventoryData = JsonSerializer.Serialize(new { slotCount = 20, usedSlots = 12 }),
                QuestFlags    = JsonSerializer.Serialize(new { tutorialCompleted = true, firstBossFight = true }),
                SavedAt       = DateTime.UtcNow.AddHours(-1),
            },
            // Kael — 1 save au boss final
            new GameSave
            {
                PlayerId      = profiles[1].Id,
                CurrentZone   = "BossFinal",
                PositionX     = 100.0f, PositionY = 150.0f,
                InventoryData = JsonSerializer.Serialize(new { slotCount = 40, usedSlots = 17 }),
                QuestFlags    = JsonSerializer.Serialize(new { tutorialCompleted = true, firstBossFight = true, finalBossUnlocked = true }),
                SavedAt       = DateTime.UtcNow.AddMinutes(-20),
            },
        };
        await context.GameSaves.AddRangeAsync(gameSaves);
        await context.SaveChangesAsync();

        // ===== 11. USERCONSENTS =====
        var userConsents = new[]
        {
            new UserConsent { UserId = users[0].Id, AnalyticsConsent = true,  MarketingConsent = false },
            new UserConsent { UserId = users[1].Id, AnalyticsConsent = true,  MarketingConsent = true  },
            new UserConsent { UserId = users[2].Id, AnalyticsConsent = false, MarketingConsent = false },
        };
        await context.UserConsents.AddRangeAsync(userConsents);
        await context.SaveChangesAsync();

        // ===== 12. AUDITLOGS =====
        var auditLogs = new[]
        {
            new AuditLog { UserId = users[0].Id, EventType = "LOGIN_SUCCESS", EventData = JsonSerializer.Serialize(new { method = "password" }), IpAddress = "127.0.0.1", UserAgent = "Mozilla/5.0", Timestamp = DateTime.UtcNow.AddHours(-1) },
            new AuditLog { UserId = users[1].Id, EventType = "LOGIN_SUCCESS", EventData = JsonSerializer.Serialize(new { method = "password" }), IpAddress = "127.0.0.1", UserAgent = "Mozilla/5.0", Timestamp = DateTime.UtcNow.AddMinutes(-30) },
            new AuditLog { UserId = users[0].Id, EventType = "DATA_EXPORT",   EventData = JsonSerializer.Serialize(new { format = "json", size = "3.1KB" }), IpAddress = "127.0.0.1", UserAgent = "Mozilla/5.0", Timestamp = DateTime.UtcNow.AddMinutes(-15) },
            new AuditLog { UserId = users[2].Id, EventType = "LOGIN_SUCCESS", EventData = JsonSerializer.Serialize(new { method = "password" }), IpAddress = "127.0.0.1", UserAgent = "Mozilla/5.0", Timestamp = DateTime.UtcNow.AddDays(-2) },
        };
        await context.AuditLogs.AddRangeAsync(auditLogs);
        await context.SaveChangesAsync();
    }

    private static string GetRequiredSeedPassword(IConfiguration? configuration, string key)
    {
        var value = configuration?[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"La variable de seed '{key}' est requise. " +
                $"Configurez-la dans les variables d'environnement.");
        return value;
    }
}