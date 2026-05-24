using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using RPG_ESI07.Domain;
using RPG_ESI07.Domain.Entities;
using RPG_ESI07.Domain.Interfaces;
using System.Text;
using System.Text.Json;

namespace RPG_ESI07.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(AppDbContext context, IPasswordHasher hasher, IConfiguration? configuration = null)
    {
        if (await context.Users.AnyAsync()) return;

        var playerPassword = GetRequiredSeedPassword(configuration, "Seed:PlayerPassword");
        var adminPassword = GetRequiredSeedPassword(configuration, "Seed:AdminPassword");
        var testPassword = GetRequiredSeedPassword(configuration, "Seed:TestPassword");

        // ═══════════════════════════════════════════════════════════
        // 1. USERS — 11 joueurs, 1 admin (ID 12 = Sathom), 8 joueurs
        // ═══════════════════════════════════════════════════════════
        var users = new[]
        {
            // ── Joueurs 1–11 ──────────────────────────────────────────
            new User { Username = "Aelindra",  Email = Encoding.UTF8.GetBytes("aelindra@rpg-esi07.com"),  PasswordHash = hasher.HashPassword(playerPassword), Role = Constants.RolePlayer, MfaEnabled = false, CreatedAt = DateTime.UtcNow.AddDays(-120), LastLoginAt = DateTime.UtcNow.AddHours(-2),  LastLoginIP = Constants.SeedIpAddress },
            new User { Username = "Borak",     Email = Encoding.UTF8.GetBytes("borak@rpg-esi07.com"),     PasswordHash = hasher.HashPassword(playerPassword), Role = Constants.RolePlayer, MfaEnabled = false, CreatedAt = DateTime.UtcNow.AddDays(-90),  LastLoginAt = DateTime.UtcNow.AddHours(-5),  LastLoginIP = Constants.SeedIpAddress },
            new User { Username = "Cyraelle",  Email = Encoding.UTF8.GetBytes("cyraelle@rpg-esi07.com"),  PasswordHash = hasher.HashPassword(playerPassword), Role = Constants.RolePlayer, MfaEnabled = true,  CreatedAt = DateTime.UtcNow.AddDays(-60),  LastLoginAt = DateTime.UtcNow.AddDays(-1),   LastLoginIP = Constants.SeedIpAddress },
            new User { Username = "Daevar",    Email = Encoding.UTF8.GetBytes("daevar@rpg-esi07.com"),    PasswordHash = hasher.HashPassword(playerPassword), Role = Constants.RolePlayer, MfaEnabled = false, CreatedAt = DateTime.UtcNow.AddDays(-200), LastLoginAt = DateTime.UtcNow.AddHours(-1),  LastLoginIP = Constants.SeedIpAddress },
            new User { Username = "Elowen",    Email = Encoding.UTF8.GetBytes("elowen@rpg-esi07.com"),    PasswordHash = hasher.HashPassword(testPassword),   Role = Constants.RolePlayer, MfaEnabled = false, CreatedAt = DateTime.UtcNow.AddDays(-3),   LastLoginAt = DateTime.UtcNow.AddHours(-20), LastLoginIP = Constants.SeedIpAddress },
            new User { Username = "Fenrisulf", Email = Encoding.UTF8.GetBytes("fenrisulf@rpg-esi07.com"), PasswordHash = hasher.HashPassword(playerPassword), Role = Constants.RolePlayer, MfaEnabled = false, CreatedAt = DateTime.UtcNow.AddDays(-150), LastLoginAt = DateTime.UtcNow.AddHours(-3),  LastLoginIP = Constants.SeedIpAddress },
            new User { Username = "Gryphael",  Email = Encoding.UTF8.GetBytes("gryphael@rpg-esi07.com"),  PasswordHash = hasher.HashPassword(playerPassword), Role = Constants.RolePlayer, MfaEnabled = true,  CreatedAt = DateTime.UtcNow.AddDays(-400), LastLoginAt = DateTime.UtcNow.AddMinutes(-30),LastLoginIP = Constants.SeedIpAddress },
            new User { Username = "Haliavar",  Email = Encoding.UTF8.GetBytes("haliavar@rpg-esi07.com"),  PasswordHash = hasher.HashPassword(playerPassword), Role = Constants.RolePlayer, MfaEnabled = false, CreatedAt = DateTime.UtcNow.AddDays(-45),  LastLoginAt = DateTime.UtcNow.AddDays(-2),   LastLoginIP = Constants.SeedIpAddress },
            new User { Username = "Irithos",   Email = Encoding.UTF8.GetBytes("irithos@rpg-esi07.com"),   PasswordHash = hasher.HashPassword(playerPassword), Role = Constants.RolePlayer, MfaEnabled = false, CreatedAt = DateTime.UtcNow.AddDays(-80),  LastLoginAt = DateTime.UtcNow.AddHours(-8),  LastLoginIP = Constants.SeedIpAddress },
            new User { Username = "Jorath",    Email = Encoding.UTF8.GetBytes("jorath@rpg-esi07.com"),    PasswordHash = hasher.HashPassword(playerPassword), Role = Constants.RolePlayer, MfaEnabled = true,  CreatedAt = DateTime.UtcNow.AddDays(-300), LastLoginAt = DateTime.UtcNow.AddHours(-4),  LastLoginIP = Constants.SeedIpAddress },
            new User { Username = "Kessavel",  Email = Encoding.UTF8.GetBytes("kessavel@rpg-esi07.com"),  PasswordHash = hasher.HashPassword(testPassword),   Role = Constants.RolePlayer, MfaEnabled = false, CreatedAt = DateTime.UtcNow.AddDays(-7),   LastLoginAt = DateTime.UtcNow.AddHours(-12), LastLoginIP = Constants.SeedIpAddress },
            // ── Admin #12 : Sathom ────────────────────────────────────
            new User { Username = "Sathom",    Email = Encoding.UTF8.GetBytes("sathom@rpg-esi07.com"),    PasswordHash = hasher.HashPassword(adminPassword),  Role = Constants.RoleAdmin,  MfaEnabled = false,  CreatedAt = DateTime.UtcNow.AddDays(-500), LastLoginAt = DateTime.UtcNow.AddMinutes(-10),LastLoginIP = Constants.SeedIpAddress },
            // ── Joueurs 13–20 ─────────────────────────────────────────
            new User { Username = "Lyrandis",  Email = Encoding.UTF8.GetBytes("lyrandis@rpg-esi07.com"),  PasswordHash = hasher.HashPassword(playerPassword), Role = Constants.RolePlayer, MfaEnabled = false, CreatedAt = DateTime.UtcNow.AddDays(-55),  LastLoginAt = DateTime.UtcNow.AddHours(-6),  LastLoginIP = Constants.SeedIpAddress },
            new User { Username = "Mareneth",  Email = Encoding.UTF8.GetBytes("mareneth@rpg-esi07.com"),  PasswordHash = hasher.HashPassword(playerPassword), Role = Constants.RolePlayer, MfaEnabled = false, CreatedAt = DateTime.UtcNow.AddDays(-130), LastLoginAt = DateTime.UtcNow.AddHours(-9),  LastLoginIP = Constants.SeedIpAddress },
            new User { Username = "Noxvaer",   Email = Encoding.UTF8.GetBytes("noxvaer@rpg-esi07.com"),   PasswordHash = hasher.HashPassword(testPassword),   Role = Constants.RolePlayer, MfaEnabled = false, CreatedAt = DateTime.UtcNow.AddDays(-5),   LastLoginAt = DateTime.UtcNow.AddHours(-18), LastLoginIP = Constants.SeedIpAddress },
            new User { Username = "Ophirath",  Email = Encoding.UTF8.GetBytes("ophirath@rpg-esi07.com"),  PasswordHash = hasher.HashPassword(playerPassword), Role = Constants.RolePlayer, MfaEnabled = true,  CreatedAt = DateTime.UtcNow.AddDays(-250), LastLoginAt = DateTime.UtcNow.AddHours(-1),  LastLoginIP = Constants.SeedIpAddress },
            new User { Username = "Phelanor",  Email = Encoding.UTF8.GetBytes("phelanor@rpg-esi07.com"),  PasswordHash = hasher.HashPassword(playerPassword), Role = Constants.RolePlayer, MfaEnabled = false, CreatedAt = DateTime.UtcNow.AddDays(-40),  LastLoginAt = DateTime.UtcNow.AddDays(-3),   LastLoginIP = Constants.SeedIpAddress },
            new User { Username = "Quillaris", Email = Encoding.UTF8.GetBytes("quillaris@rpg-esi07.com"), PasswordHash = hasher.HashPassword(playerPassword), Role = Constants.RolePlayer, MfaEnabled = false, CreatedAt = DateTime.UtcNow.AddDays(-100), LastLoginAt = DateTime.UtcNow.AddHours(-7),  LastLoginIP = Constants.SeedIpAddress },
            new User { Username = "Ravenskar", Email = Encoding.UTF8.GetBytes("ravenskar@rpg-esi07.com"), PasswordHash = hasher.HashPassword(playerPassword), Role = Constants.RolePlayer, MfaEnabled = true,  CreatedAt = DateTime.UtcNow.AddDays(-180), LastLoginAt = DateTime.UtcNow.AddHours(-2),  LastLoginIP = Constants.SeedIpAddress },
            new User { Username = "Selvynor",  Email = Encoding.UTF8.GetBytes("selvynor@rpg-esi07.com"),  PasswordHash = hasher.HashPassword(testPassword),   Role = Constants.RolePlayer, MfaEnabled = false, CreatedAt = DateTime.UtcNow.AddDays(-15),  LastLoginAt = DateTime.UtcNow.AddHours(-10), LastLoginIP = Constants.SeedIpAddress },
        };
        await context.Users.AddRangeAsync(users);
        await context.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════
        // 2. PLAYER PROFILES
        // Niveaux variés — stats cohérentes avec HP=100+(lvl-1)*10, Str=10+(lvl-1)*2
        // ═══════════════════════════════════════════════════════════
        var profiles = new[]
        {
            // [0] Aelindra — mage niv.15, spé magie
            new PlayerProfile { UserId=users[0].Id,  CharacterName="Aelindra",        Level=15, CurrentHP=160,  MaxHP=240,   CurrentMP=420, MaxMP=420, Strength=8,  Intelligence=48, Speed=14, Experience=14200,  Gold=3200,  UpdatedAt=DateTime.UtcNow.AddHours(-2)  },
            // [1] Borak — guerrier niv.32, spé force
            new PlayerProfile { UserId=users[1].Id,  CharacterName="Borak le Briseur",Level=32, CurrentHP=410,  MaxHP=410,   CurrentMP=60,  MaxMP=60,  Strength=68, Intelligence=12, Speed=9,  Experience=68000,  Gold=12000, UpdatedAt=DateTime.UtcNow.AddHours(-5)  },
            // [2] Cyraelle — archère niv.7, spé vitesse
            new PlayerProfile { UserId=users[2].Id,  CharacterName="Cyraelle",        Level=7,  CurrentHP=100,  MaxHP=160,   CurrentMP=90,  MaxMP=90,  Strength=14, Intelligence=16, Speed=30, Experience=2800,   Gold=650,   UpdatedAt=DateTime.UtcNow.AddDays(-1)   },
            // [3] Daevar — paladin niv.55, équilibré
            new PlayerProfile { UserId=users[3].Id,  CharacterName="Daevar Soleil",   Level=55, CurrentHP=640,  MaxHP=640,   CurrentMP=200, MaxMP=200, Strength=90, Intelligence=35, Speed=16, Experience=320000, Gold=45000, UpdatedAt=DateTime.UtcNow.AddHours(-1)  },
            // [4] Elowen — débutant niv.1
            new PlayerProfile { UserId=users[4].Id,  CharacterName="Elowen",          Level=1,  CurrentHP=100,  MaxHP=100,   CurrentMP=50,  MaxMP=50,  Strength=10, Intelligence=10, Speed=10, Experience=0,      Gold=50,    UpdatedAt=DateTime.UtcNow.AddHours(-20) },
            // [5] Fenrisulf — barbare niv.43, spé force/vit
            new PlayerProfile { UserId=users[5].Id,  CharacterName="Fenrisulf",       Level=43, CurrentHP=520,  MaxHP=520,   CurrentMP=40,  MaxMP=40,  Strength=96, Intelligence=8,  Speed=22, Experience=198000, Gold=22000, UpdatedAt=DateTime.UtcNow.AddHours(-3)  },
            // [6] Gryphael — grand mage niv.88
            new PlayerProfile { UserId=users[6].Id,  CharacterName="Gryphael l'Arcane",Level=88, CurrentHP=970, MaxHP=970,  CurrentMP=900, MaxMP=900, Strength=30, Intelligence=95, Speed=20, Experience=980000, Gold=120000,UpdatedAt=DateTime.UtcNow.AddMinutes(-30)},
            // [7] Haliavar — roublard niv.12
            new PlayerProfile { UserId=users[7].Id,  CharacterName="Haliavar",        Level=12, CurrentHP=190,  MaxHP=210,   CurrentMP=80,  MaxMP=80,  Strength=22, Intelligence=14, Speed=28, Experience=7200,   Gold=1800,  UpdatedAt=DateTime.UtcNow.AddDays(-2)   },
            // [8] Irithos — nécromancien niv.24
            new PlayerProfile { UserId=users[8].Id,  CharacterName="Irithos le Pâle", Level=24, CurrentHP=280,  MaxHP=330,   CurrentMP=380, MaxMP=380, Strength=20, Intelligence=55, Speed=12, Experience=36000,  Gold=8500,  UpdatedAt=DateTime.UtcNow.AddHours(-8)  },
            // [9] Jorath — tank niv.67
            new PlayerProfile { UserId=users[9].Id,  CharacterName="Jorath Roc",      Level=67, CurrentHP=760,  MaxHP=760,   CurrentMP=100, MaxMP=100, Strength=110,Intelligence=18, Speed=8,  Experience=580000, Gold=78000, UpdatedAt=DateTime.UtcNow.AddHours(-4)  },
            // [10] Kessavel — novice niv.3
            new PlayerProfile { UserId=users[10].Id, CharacterName="Kessavel",        Level=3,  CurrentHP=120,  MaxHP=120,   CurrentMP=60,  MaxMP=60,  Strength=14, Intelligence=12, Speed=12, Experience=480,    Gold=120,   UpdatedAt=DateTime.UtcNow.AddHours(-12) },
            // [11] Sathom — admin niv.99
            new PlayerProfile { UserId=users[11].Id, CharacterName="Sathom",          Level=99, CurrentHP=1080, MaxHP=1080,  CurrentMP=1000,MaxMP=1000,Strength=206,Intelligence=100,Speed=50, Experience=9999999,Gold=999999,UpdatedAt=DateTime.UtcNow.AddMinutes(-10)},
            // [12] Lyrandis — druide niv.19
            new PlayerProfile { UserId=users[12].Id, CharacterName="Lyrandis",        Level=19, CurrentHP=220,  MaxHP=280,   CurrentMP=260, MaxMP=260, Strength=22, Intelligence=40, Speed=18, Experience=22000,  Gold=5200,  UpdatedAt=DateTime.UtcNow.AddHours(-6)  },
            // [13] Mareneth — guerrière niv.38
            new PlayerProfile { UserId=users[13].Id, CharacterName="Mareneth l'Acier",Level=38, CurrentHP=470,  MaxHP=470,   CurrentMP=80,  MaxMP=80,  Strength=80, Intelligence=15, Speed=14, Experience=142000, Gold=18000, UpdatedAt=DateTime.UtcNow.AddHours(-9)  },
            // [14] Noxvaer — débutant niv.5
            new PlayerProfile { UserId=users[14].Id, CharacterName="Noxvaer",         Level=5,  CurrentHP=140,  MaxHP=140,   CurrentMP=50,  MaxMP=50,  Strength=18, Intelligence=10, Speed=12, Experience=800,    Gold=200,   UpdatedAt=DateTime.UtcNow.AddHours(-18) },
            // [15] Ophirath — enchanteresse niv.72
            new PlayerProfile { UserId=users[15].Id, CharacterName="Ophirath Lune",   Level=72, CurrentHP=810,  MaxHP=810,   CurrentMP=800, MaxMP=800, Strength=35, Intelligence=90, Speed=25, Experience=680000, Gold=95000, UpdatedAt=DateTime.UtcNow.AddHours(-1)  },
            // [16] Phelanor — chasseur niv.16
            new PlayerProfile { UserId=users[16].Id, CharacterName="Phelanor",        Level=16, CurrentHP=240,  MaxHP=250,   CurrentMP=100, MaxMP=100, Strength=32, Intelligence=14, Speed=26, Experience=16000,  Gold=3800,  UpdatedAt=DateTime.UtcNow.AddDays(-3)   },
            // [17] Quillaris — barde niv.29
            new PlayerProfile { UserId=users[17].Id, CharacterName="Quillaris le Songe",Level=29,CurrentHP=370, MaxHP=380,  CurrentMP=200, MaxMP=200, Strength=42, Intelligence=30, Speed=20, Experience=52000,  Gold=11000, UpdatedAt=DateTime.UtcNow.AddHours(-7)  },
            // [18] Ravenskar — assassin niv.51
            new PlayerProfile { UserId=users[18].Id, CharacterName="Ravenskar",       Level=51, CurrentHP=550,  MaxHP=600,   CurrentMP=150, MaxMP=150, Strength=88, Intelligence=20, Speed=38, Experience=270000, Gold=38000, UpdatedAt=DateTime.UtcNow.AddHours(-2)  },
            // [19] Selvynor — novice niv.9
            new PlayerProfile { UserId=users[19].Id, CharacterName="Selvynor",        Level=9,  CurrentHP=160,  MaxHP=180,   CurrentMP=70,  MaxMP=70,  Strength=20, Intelligence=14, Speed=16, Experience=4200,   Gold=450,   UpdatedAt=DateTime.UtcNow.AddHours(-10) },
        };
        await context.PlayerProfiles.AddRangeAsync(profiles);
        await context.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════
        // 3. ENEMIES (70) — Stats calibrées pour le scaling dynamique
        //    Formule : mult = 1 + (maxMult-1)/(1+exp(-k*(ratio-1)))
        //    basic:   basePower=50,  maxMult=8,  k=2.5
        //    miniboss:basePower=150, maxMult=12, k=2.0
        //    boss:    basePower=400, maxMult=20, k=1.5
        //
        //    Base stats = stats à mult=1 (jamais atteint en pratique)
        //    Sweet spot basic   ≈ lvl 5  (ratio=1, mult≈4.5)
        //    Sweet spot miniboss≈ lvl 15 (ratio=1, mult≈6.5)
        //    Sweet spot boss    ≈ lvl 40 (ratio=1, mult≈10.5)
        // ═══════════════════════════════════════════════════════════
        var enemies = new[]
        {
            // ═══ BASIC (40) ═══════════════════════════════════════

            // ── Forêt & Nature ────────────────────────────────────
            new Enemy { Name="Gobelin des Bois",      Type=Constants.EnemyTypeBasic, MaxHP=28, Strength=4,  Intelligence=3,  Speed=10, PhysicalResistance=1.0f, MagicalResistance=1.2f, ExperienceReward=12, GoldReward=6,  Description="Petite créature rusée tendant des embuscades sous la canopée",       InfluenceRadius=4f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Rat Géant",             Type=Constants.EnemyTypeBasic, MaxHP=20, Strength=3,  Intelligence=2,  Speed=16, PhysicalResistance=1.0f, MagicalResistance=1.0f, ExperienceReward=8,  GoldReward=3,  Description="Rongeur mutant dont les dents percent le cuir sans effort",          InfluenceRadius=3f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Loup des Cendres",      Type=Constants.EnemyTypeBasic, MaxHP=38, Strength=7,  Intelligence=4,  Speed=20, PhysicalResistance=1.0f, MagicalResistance=1.0f, ExperienceReward=22, GoldReward=10, Description="Prédateur au pelage noirci par la magie corrompue",                   InfluenceRadius=6f, InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Araignée Venimeuse",    Type=Constants.EnemyTypeBasic, MaxHP=32, Strength=5,  Intelligence=3,  Speed=18, PhysicalResistance=1.1f, MagicalResistance=0.9f, ExperienceReward=18, GoldReward=8,  Description="Son venin paralyse sa proie avant de la coconner",                   InfluenceRadius=4f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Sanglier Maudit",       Type=Constants.EnemyTypeBasic, MaxHP=55, Strength=9,  Intelligence=2,  Speed=12, PhysicalResistance=0.8f, MagicalResistance=1.2f, ExperienceReward=28, GoldReward=12, Description="Charge aveuglément, insensible à la douleur par la malédiction",     InfluenceRadius=5f, InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Guêpe Géante",          Type=Constants.EnemyTypeBasic, MaxHP=25, Strength=5,  Intelligence=3,  Speed=22, PhysicalResistance=1.2f, MagicalResistance=1.0f, ExperienceReward=15, GoldReward=5,  Description="Insecte surdimensionné dont le dard injecte un venin puissant",      InfluenceRadius=5f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Dryade Corrompue",      Type=Constants.EnemyTypeBasic, MaxHP=45, Strength=6,  Intelligence=12, Speed=8,  PhysicalResistance=1.3f, MagicalResistance=0.7f, ExperienceReward=25, GoldReward=14, Description="Esprit de la forêt corrompu par la magie noire, attaque à distance",  InfluenceRadius=6f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Scarabée de Pierre",    Type=Constants.EnemyTypeBasic, MaxHP=50, Strength=6,  Intelligence=1,  Speed=6,  PhysicalResistance=0.6f, MagicalResistance=1.4f, ExperienceReward=20, GoldReward=8,  Description="Insecte blindé dont la carapace dévie la plupart des coups physiques",InfluenceRadius=3f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Fauve des Brumes",      Type=Constants.EnemyTypeBasic, MaxHP=42, Strength=8,  Intelligence=5,  Speed=17, PhysicalResistance=1.0f, MagicalResistance=1.1f, ExperienceReward=24, GoldReward=11, Description="Félin semi-éthéré se dissimulant dans le brouillard pour attaquer",   InfluenceRadius=7f, InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Lierre Rampant",        Type=Constants.EnemyTypeBasic, MaxHP=60, Strength=5,  Intelligence=2,  Speed=3,  PhysicalResistance=0.9f, MagicalResistance=1.1f, ExperienceReward=16, GoldReward=4,  Description="Plante animée immobilisant ses proies avec ses vrilles acérées",      InfluenceRadius=3f, InitialState=Constants.EnemyStateRepos },

            // ── Morts-Vivants ─────────────────────────────────────
            new Enemy { Name="Squelette Guerrier",    Type=Constants.EnemyTypeBasic, MaxHP=35, Strength=7,  Intelligence=2,  Speed=9,  PhysicalResistance=0.7f, MagicalResistance=1.3f, ExperienceReward=20, GoldReward=10, Description="Armature osseuse animée maniant encore l'épée avec compétence",       InfluenceRadius=4f, InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Zombie Putride",        Type=Constants.EnemyTypeBasic, MaxHP=65, Strength=8,  Intelligence=1,  Speed=3,  PhysicalResistance=0.6f, MagicalResistance=1.4f, ExperienceReward=22, GoldReward=4,  Description="Mort-vivant lent mais quasi insensible aux dégâts physiques",         InfluenceRadius=3f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Fantôme Errant",        Type=Constants.EnemyTypeBasic, MaxHP=30, Strength=6,  Intelligence=8,  Speed=12, PhysicalResistance=1.8f, MagicalResistance=0.5f, ExperienceReward=25, GoldReward=8,  Description="Esprit désincarné quasi immunisé aux attaques physiques",             InfluenceRadius=5f, InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Crâne Enflammé",        Type=Constants.EnemyTypeBasic, MaxHP=22, Strength=4,  Intelligence=14, Speed=14, PhysicalResistance=1.2f, MagicalResistance=0.6f, ExperienceReward=20, GoldReward=9,  Description="Tête de mort animée crachant des flammes nécrotiques",                InfluenceRadius=4f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Spectre Mineur",        Type=Constants.EnemyTypeBasic, MaxHP=28, Strength=5,  Intelligence=10, Speed=10, PhysicalResistance=1.6f, MagicalResistance=0.6f, ExperienceReward=22, GoldReward=7,  Description="Ombre translucide drainant la chaleur vitale de ses victimes",        InfluenceRadius=5f, InitialState=Constants.EnemyStateRepos },

            // ── Humanoïdes & Brigands ─────────────────────────────
            new Enemy { Name="Bandit Masqué",         Type=Constants.EnemyTypeBasic, MaxHP=45, Strength=8,  Intelligence=7,  Speed=13, PhysicalResistance=1.0f, MagicalResistance=1.0f, ExperienceReward=26, GoldReward=20, Description="Ancien soldat reconverti dans le pillage après la guerre",            InfluenceRadius=5f, InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Cultiste Fou",          Type=Constants.EnemyTypeBasic, MaxHP=35, Strength=5,  Intelligence=15, Speed=8,  PhysicalResistance=1.1f, MagicalResistance=0.8f, ExperienceReward=24, GoldReward=15, Description="Fanatique ayant sacrifié sa santé mentale pour un pouvoir sombre",     InfluenceRadius=4f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Orc Guerrier",          Type=Constants.EnemyTypeBasic, MaxHP=58, Strength=12, Intelligence=3,  Speed=7,  PhysicalResistance=0.9f, MagicalResistance=1.1f, ExperienceReward=30, GoldReward=18, Description="Guerrier brutal dont la rage compense le manque de stratégie",         InfluenceRadius=5f, InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Gobelin Chamane",       Type=Constants.EnemyTypeBasic, MaxHP=25, Strength=3,  Intelligence=18, Speed=9,  PhysicalResistance=1.2f, MagicalResistance=0.7f, ExperienceReward=28, GoldReward=16, Description="Sorcier gobelin maîtrisant les malédictions et les poisons élémentaires",InfluenceRadius=5f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Mercenaire Renégat",    Type=Constants.EnemyTypeBasic, MaxHP=52, Strength=10, Intelligence=8,  Speed=11, PhysicalResistance=0.9f, MagicalResistance=1.0f, ExperienceReward=32, GoldReward=25, Description="Guerrier expérimenté combattant maintenant pour le plus offrant",      InfluenceRadius=5f, InitialState=Constants.EnemyStatePatrouille },

            // ── Élémentaires & Magiques ───────────────────────────
            new Enemy { Name="Imp Pyromaniac",        Type=Constants.EnemyTypeBasic, MaxHP=24, Strength=4,  Intelligence=16, Speed=15, PhysicalResistance=1.3f, MagicalResistance=0.5f, ExperienceReward=22, GoldReward=14, Description="Petit démon obsédé par les flammes qu'il projette en toutes directions",InfluenceRadius=5f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Élémentaire de Terre",  Type=Constants.EnemyTypeBasic, MaxHP=70, Strength=10, Intelligence=2,  Speed=4,  PhysicalResistance=0.5f, MagicalResistance=1.5f, ExperienceReward=28, GoldReward=10, Description="Agrégat de roche animé, lent mais extrêmement résistant physiquement", InfluenceRadius=3f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Slime Acide",           Type=Constants.EnemyTypeBasic, MaxHP=40, Strength=5,  Intelligence=2,  Speed=5,  PhysicalResistance=0.6f, MagicalResistance=1.4f, ExperienceReward=18, GoldReward=6,  Description="Créature amorphe corrodant tout ce qu'elle touche",                  InfluenceRadius=3f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Feu Follet",            Type=Constants.EnemyTypeBasic, MaxHP=18, Strength=3,  Intelligence=12, Speed=20, PhysicalResistance=1.7f, MagicalResistance=0.5f, ExperienceReward=20, GoldReward=8,  Description="Sphère lumineuse erratique attirant les voyageurs vers leur perte",   InfluenceRadius=6f, InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Gobelours",             Type=Constants.EnemyTypeBasic, MaxHP=62, Strength=11, Intelligence=4,  Speed=8,  PhysicalResistance=0.9f, MagicalResistance=1.1f, ExperienceReward=30, GoldReward=15, Description="Hybride gobelin-ours d'une force brute considérable",                  InfluenceRadius=5f, InitialState=Constants.EnemyStatePatrouille },

            // ── Aquatiques ────────────────────────────────────────
            new Enemy { Name="Crabe Géant",           Type=Constants.EnemyTypeBasic, MaxHP=55, Strength=8,  Intelligence=2,  Speed=7,  PhysicalResistance=0.6f, MagicalResistance=1.4f, ExperienceReward=24, GoldReward=12, Description="Crustacé dont la carapace résiste aux coups les plus puissants",       InfluenceRadius=4f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Serpent des Eaux",      Type=Constants.EnemyTypeBasic, MaxHP=38, Strength=7,  Intelligence=4,  Speed=16, PhysicalResistance=1.0f, MagicalResistance=1.0f, ExperienceReward=20, GoldReward=9,  Description="Reptile aquatique constricting ses proies sous les flots",            InfluenceRadius=5f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Méduse Électrique",     Type=Constants.EnemyTypeBasic, MaxHP=30, Strength=3,  Intelligence=14, Speed=8,  PhysicalResistance=1.1f, MagicalResistance=0.8f, ExperienceReward=22, GoldReward=10, Description="Créature translucide déchargeant de l'électricité au contact",        InfluenceRadius=4f, InitialState=Constants.EnemyStateRepos },

            // ── Cavernes ──────────────────────────────────────────
            new Enemy { Name="Chauve-Souris Géante",  Type=Constants.EnemyTypeBasic, MaxHP=28, Strength=5,  Intelligence=3,  Speed=22, PhysicalResistance=1.1f, MagicalResistance=0.9f, ExperienceReward=16, GoldReward=5,  Description="Chiroptère massif naviguant par écholocation dans les grottes sombres",InfluenceRadius=6f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Ver de Pierre",         Type=Constants.EnemyTypeBasic, MaxHP=75, Strength=9,  Intelligence=1,  Speed=5,  PhysicalResistance=0.7f, MagicalResistance=1.3f, ExperienceReward=25, GoldReward=7,  Description="Lombric collossal creusant la roche, surgissant sans prévenir",        InfluenceRadius=4f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Champignon Explosif",   Type=Constants.EnemyTypeBasic, MaxHP=20, Strength=6,  Intelligence=1,  Speed=2,  PhysicalResistance=1.2f, MagicalResistance=0.8f, ExperienceReward=18, GoldReward=4,  Description="Fongique libérant une explosion de spores toxiques à sa mort",         InfluenceRadius=2f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Taupe Alchimique",      Type=Constants.EnemyTypeBasic, MaxHP=45, Strength=7,  Intelligence=6,  Speed=8,  PhysicalResistance=0.8f, MagicalResistance=1.2f, ExperienceReward=22, GoldReward=10, Description="Rongeur souterrain dont les griffes sécrètent un acide corrosif",      InfluenceRadius=4f, InitialState=Constants.EnemyStateRepos },

            // ── Divers ────────────────────────────────────────────
            new Enemy { Name="Automate Défaillant",   Type=Constants.EnemyTypeBasic, MaxHP=48, Strength=8,  Intelligence=5,  Speed=6,  PhysicalResistance=0.6f, MagicalResistance=1.4f, ExperienceReward=26, GoldReward=15, Description="Golem mécanique dont les rouages endommagés causent des mouvements erratiques",InfluenceRadius=4f, InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Gnoll Archer",          Type=Constants.EnemyTypeBasic, MaxHP=40, Strength=7,  Intelligence=6,  Speed=11, PhysicalResistance=1.0f, MagicalResistance=1.0f, ExperienceReward=25, GoldReward=14, Description="Hyène humanoïde dont la précision au tir est redoutable à distance",   InfluenceRadius=8f, InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Kobold Foudroyant",     Type=Constants.EnemyTypeBasic, MaxHP=22, Strength=3,  Intelligence=15, Speed=18, PhysicalResistance=1.1f, MagicalResistance=0.8f, ExperienceReward=20, GoldReward=11, Description="Lézard humanoïde maîtrisant la magie des orages",                      InfluenceRadius=5f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Gargoyle de Pierre",    Type=Constants.EnemyTypeBasic, MaxHP=58, Strength=9,  Intelligence=4,  Speed=8,  PhysicalResistance=0.5f, MagicalResistance=1.5f, ExperienceReward=28, GoldReward=13, Description="Statue animée dont la roche constitue une armure naturelle",           InfluenceRadius=5f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Gremlin Voleur",        Type=Constants.EnemyTypeBasic, MaxHP=22, Strength=4,  Intelligence=9,  Speed=20, PhysicalResistance=1.1f, MagicalResistance=0.9f, ExperienceReward=18, GoldReward=18, Description="Petit démon kleptopathe déroutant ses adversaires par sa mobilité",    InfluenceRadius=4f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Ombre Mineure",         Type=Constants.EnemyTypeBasic, MaxHP=26, Strength=6,  Intelligence=8,  Speed=14, PhysicalResistance=1.6f, MagicalResistance=0.5f, ExperienceReward=22, GoldReward=9,  Description="Fragment d'obscurité pure se déplaçant dans les zones d'ombre",        InfluenceRadius=5f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Pillard des Dunes",     Type=Constants.EnemyTypeBasic, MaxHP=44, Strength=9,  Intelligence=6,  Speed=10, PhysicalResistance=1.0f, MagicalResistance=1.0f, ExperienceReward=26, GoldReward=20, Description="Brigand du désert équipé de sable envoûté qu'il projette dans les yeux",InfluenceRadius=5f, InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Spectre de Rouille",    Type=Constants.EnemyTypeBasic, MaxHP=32, Strength=5,  Intelligence=10, Speed=11, PhysicalResistance=1.5f, MagicalResistance=0.6f, ExperienceReward=24, GoldReward=6,  Description="Esprit corrosif dégradant les armures et armes au contact",           InfluenceRadius=5f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Guerrier de Boue",      Type=Constants.EnemyTypeBasic, MaxHP=68, Strength=8,  Intelligence=1,  Speed=4,  PhysicalResistance=0.8f, MagicalResistance=1.2f, ExperienceReward=22, GoldReward=5,  Description="Golem primitif formé de terre et d'eau stagnante corrompue",           InfluenceRadius=3f, InitialState=Constants.EnemyStateRepos },

            // ═══ MINIBOSS (20) ════════════════════════════════════
            // sweet spot lvl ~15 : mult≈6.5x
            // base stats → scaled: HP*6.5, Str*6.5

            // ── Magiques ──────────────────────────────────────────
            new Enemy { Name="Mage Corrompu",         Type=Constants.EnemyTypeMiniboss, MaxHP=80,  Strength=6,  Intelligence=30, Speed=8,  PhysicalResistance=1.6f, MagicalResistance=0.5f, ExperienceReward=120, GoldReward=65,  Description="Érudit vendu aux ténèbres pour un pouvoir illimité",               InfluenceRadius=8f,  InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Archimage du Vide",     Type=Constants.EnemyTypeMiniboss, MaxHP=90,  Strength=5,  Intelligence=38, Speed=7,  PhysicalResistance=1.7f, MagicalResistance=0.5f, ExperienceReward=145, GoldReward=80,  Description="Sorcier maîtrisant le vide inter-dimensionnel pour détruire la matière",InfluenceRadius=9f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Nécromancien Mineur",   Type=Constants.EnemyTypeMiniboss, MaxHP=75,  Strength=6,  Intelligence=32, Speed=7,  PhysicalResistance=1.4f, MagicalResistance=0.5f, ExperienceReward=130, GoldReward=70,  Description="Apprenti liche invoquant des légions de morts pour se protéger",   InfluenceRadius=8f,  InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Sorcier Vampirique",    Type=Constants.EnemyTypeMiniboss, MaxHP=95,  Strength=10, Intelligence=28, Speed=12, PhysicalResistance=1.3f, MagicalResistance=0.6f, ExperienceReward=155, GoldReward=90,  Description="Hybride mage-vampire drainant la magie de ses ennemis",            InfluenceRadius=7f,  InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Invocateur Maudit",     Type=Constants.EnemyTypeMiniboss, MaxHP=70,  Strength=5,  Intelligence=35, Speed=6,  PhysicalResistance=1.5f, MagicalResistance=0.5f, ExperienceReward=140, GoldReward=75,  Description="Mage convoquant des démons depuis des plans d'existence alternatifs",InfluenceRadius=9f,  InitialState=Constants.EnemyStateRepos },

            // ── Physiques ─────────────────────────────────────────
            new Enemy { Name="Golem de Fer",          Type=Constants.EnemyTypeMiniboss, MaxHP=130, Strength=20, Intelligence=2,  Speed=3,  PhysicalResistance=0.5f, MagicalResistance=1.6f, ExperienceReward=150, GoldReward=75,  Description="Automate de guerre oublié, sa conscience s'est éteinte depuis longtemps",InfluenceRadius=5f,InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Chevalier Maudit",      Type=Constants.EnemyTypeMiniboss, MaxHP=110, Strength=22, Intelligence=10, Speed=10, PhysicalResistance=0.5f, MagicalResistance=1.5f, ExperienceReward=148, GoldReward=80,  Description="Paladin déchu lié à son armure pour l'éternité par une malédiction",  InfluenceRadius=6f,  InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Champion Orc",          Type=Constants.EnemyTypeMiniboss, MaxHP=120, Strength=24, Intelligence=5,  Speed=8,  PhysicalResistance=0.8f, MagicalResistance=1.2f, ExperienceReward=142, GoldReward=72,  Description="Chef de guerre orc dont la brutalité force le respect même de ses alliés",InfluenceRadius=6f,InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Géant des Glaces",      Type=Constants.EnemyTypeMiniboss, MaxHP=150, Strength=22, Intelligence=8,  Speed=5,  PhysicalResistance=0.9f, MagicalResistance=0.9f, ExperienceReward=160, GoldReward=85,  Description="Colosse de chair et de givre dont chaque pas fissure la roche",       InfluenceRadius=7f,  InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Troll Ancestral",       Type=Constants.EnemyTypeMiniboss, MaxHP=140, Strength=20, Intelligence=3,  Speed=6,  PhysicalResistance=0.7f, MagicalResistance=1.3f, ExperienceReward=145, GoldReward=70,  Description="Troll millénaire dont la régénération en fait un adversaire épuisant",  InfluenceRadius=6f,  InitialState=Constants.EnemyStatePatrouille },

            // ── Hybrides ──────────────────────────────────────────
            new Enemy { Name="Vampire Ancien",        Type=Constants.EnemyTypeMiniboss, MaxHP=100, Strength=18, Intelligence=25, Speed=20, PhysicalResistance=1.3f, MagicalResistance=0.7f, ExperienceReward=165, GoldReward=90,  Description="Seigneur de la nuit dont chaque coup vole la vitalité des victimes",  InfluenceRadius=7f,  InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Wyverne des Pics",      Type=Constants.EnemyTypeMiniboss, MaxHP=155, Strength=20, Intelligence=8,  Speed=22, PhysicalResistance=0.9f, MagicalResistance=1.1f, ExperienceReward=175, GoldReward=100, Description="Dragon immature dont le venin acide dévore l'acier comme du tissu",    InfluenceRadius=10f, InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Basilic de Pierre",     Type=Constants.EnemyTypeMiniboss, MaxHP=125, Strength=18, Intelligence=5,  Speed=7,  PhysicalResistance=0.7f, MagicalResistance=1.3f, ExperienceReward=155, GoldReward=85,  Description="Reptile légendaire pétrifirant du regard tout ce qui le fixe",         InfluenceRadius=7f,  InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Chimère Imparfaite",    Type=Constants.EnemyTypeMiniboss, MaxHP=110, Strength=16, Intelligence=15, Speed=14, PhysicalResistance=0.9f, MagicalResistance=0.9f, ExperienceReward=160, GoldReward=88,  Description="Expérience magique ratée fusionnant plusieurs créatures en une seule",  InfluenceRadius=8f,  InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Béhémoth de Terre",     Type=Constants.EnemyTypeMiniboss, MaxHP=170, Strength=25, Intelligence=2,  Speed=4,  PhysicalResistance=0.5f, MagicalResistance=1.5f, ExperienceReward=168, GoldReward=78,  Description="Mastodonte tellurique dont le simple déplacement cause des séismes",    InfluenceRadius=6f,  InitialState=Constants.EnemyStatePatrouille },

            // ── Morts-Vivants élite ───────────────────────────────
            new Enemy { Name="Roi Liche Mineur",      Type=Constants.EnemyTypeMiniboss, MaxHP=105, Strength=8,  Intelligence=40, Speed=8,  PhysicalResistance=1.4f, MagicalResistance=0.5f, ExperienceReward=175, GoldReward=95,  Description="Phylactère ambulant d'un sorcier mort refusant le trépas éternel",    InfluenceRadius=9f,  InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Seigneur Squelette",    Type=Constants.EnemyTypeMiniboss, MaxHP=100, Strength=18, Intelligence=12, Speed=10, PhysicalResistance=0.6f, MagicalResistance=1.4f, ExperienceReward=152, GoldReward=82,  Description="Général squelettique commandant une armée de morts avec une tactique redoutable",InfluenceRadius=8f,InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Banshee Hurlante",      Type=Constants.EnemyTypeMiniboss, MaxHP=80,  Strength=5,  Intelligence=35, Speed=15, PhysicalResistance=1.7f, MagicalResistance=0.5f, ExperienceReward=158, GoldReward=78,  Description="Âme en peine dont le hurlement cause des dégâts directs sur l'esprit", InfluenceRadius=9f,  InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Dullahan Sans Tête",    Type=Constants.EnemyTypeMiniboss, MaxHP=120, Strength=22, Intelligence=8,  Speed=18, PhysicalResistance=0.8f, MagicalResistance=1.2f, ExperienceReward=162, GoldReward=90,  Description="Cavalier décapité portant sa tête comme une lanterne magique",         InfluenceRadius=8f,  InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Nécros de la Ruine",    Type=Constants.EnemyTypeMiniboss, MaxHP=90,  Strength=7,  Intelligence=38, Speed=9,  PhysicalResistance=1.5f, MagicalResistance=0.5f, ExperienceReward=170, GoldReward=92,  Description="Entité née de la corruption d'un ancien sanctuaire sacré profané",      InfluenceRadius=9f,  InitialState=Constants.EnemyStateRepos },

            // ═══ BOSS (10) ════════════════════════════════════════
            // sweet spot lvl ~40 : mult≈10.5x
            // base stats → scaled: HP*10.5, Str*10.5

            new Enemy { Name="Archiliches Tenebris",  Type=Constants.EnemyTypeBoss, MaxHP=300, Strength=10, Intelligence=55, Speed=10, PhysicalResistance=1.5f, MagicalResistance=0.5f, ExperienceReward=900,  GoldReward=400, Description="Nécromancien ayant transcendé la mort, tissant la réalité à sa volonté",  InfluenceRadius=15f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Dragon de l'Aube",      Type=Constants.EnemyTypeBoss, MaxHP=420, Strength=30, Intelligence=25, Speed=20, PhysicalResistance=0.7f, MagicalResistance=0.7f, ExperienceReward=1100, GoldReward=550, Description="Dernier représentant d'une race de dragons solaires, gardien du sanctuaire",InfluenceRadius=20f, InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Le Dévoreur",           Type=Constants.EnemyTypeBoss, MaxHP=550, Strength=40, Intelligence=15, Speed=8,  PhysicalResistance=0.6f, MagicalResistance=0.6f, ExperienceReward=1300, GoldReward=450, Description="Entité primordiale sans forme définie absorbant toute énergie vitale",    InfluenceRadius=18f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Seigneur des Ombres",   Type=Constants.EnemyTypeBoss, MaxHP=350, Strength=28, Intelligence=42, Speed=28, PhysicalResistance=1.0f, MagicalResistance=0.5f, ExperienceReward=1000, GoldReward=480, Description="Maître de l'illusion se battant simultanément dans plusieurs dimensions",  InfluenceRadius=18f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Titan de Pierre",       Type=Constants.EnemyTypeBoss, MaxHP=700, Strength=50, Intelligence=5,  Speed=3,  PhysicalResistance=0.5f, MagicalResistance=1.7f, ExperienceReward=1500, GoldReward=620, Description="Colosse millénaire réveillé, dont chaque pas ébranle les fondations",      InfluenceRadius=20f, InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Hydre Légendaire",      Type=Constants.EnemyTypeBoss, MaxHP=500, Strength=35, Intelligence=12, Speed=12, PhysicalResistance=0.8f, MagicalResistance=0.8f, ExperienceReward=1200, GoldReward=500, Description="Serpent à neuf têtes dont chaque tête coupée est remplacée par deux nouvelles",InfluenceRadius=18f,InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Phénix des Cendres",    Type=Constants.EnemyTypeBoss, MaxHP=320, Strength=22, Intelligence=38, Speed=35, PhysicalResistance=0.9f, MagicalResistance=0.5f, ExperienceReward=1050, GoldReward=520, Description="Oiseau immortel renaissant de ses cendres, maîtrisant le feu et la lumière",InfluenceRadius=20f, InitialState=Constants.EnemyStatePatrouille },
            new Enemy { Name="Roi des Abysses",       Type=Constants.EnemyTypeBoss, MaxHP=460, Strength=38, Intelligence=30, Speed=10, PhysicalResistance=0.7f, MagicalResistance=0.7f, ExperienceReward=1150, GoldReward=560, Description="Entité des profondeurs marines régnant sur un royaume sous-marin de terreur",InfluenceRadius=18f,InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Gardien de l'Éternité", Type=Constants.EnemyTypeBoss, MaxHP=400, Strength=32, Intelligence=45, Speed=15, PhysicalResistance=0.8f, MagicalResistance=0.6f, ExperienceReward=1080, GoldReward=530, Description="Être hors du temps gardant les portes entre les plans d'existence",         InfluenceRadius=18f, InitialState=Constants.EnemyStateRepos },
            new Enemy { Name="Leviathan",             Type=Constants.EnemyTypeBoss, MaxHP=650, Strength=48, Intelligence=20, Speed=6,  PhysicalResistance=0.6f, MagicalResistance=0.8f, ExperienceReward=1400, GoldReward=600, Description="Titan aquatique dont la simple présence crée des vagues dévastatrices",    InfluenceRadius=22f, InitialState=Constants.EnemyStateRepos },
        };
        await context.Enemies.AddRangeAsync(enemies);
        await context.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════
        // 4. ITEMS — 10 par catégorie + consommables
        // EffectValue = contribution au bonus équipement (calculateur)
        // ═══════════════════════════════════════════════════════════
        var items = new[]
        {
            // ── Armes (10) ────────────────────────────────────────
            new Item { Name="Couteau Brisé",           Type=Constants.ItemTypeWeapon, EffectValue=5,  StatModifiers=JsonSerializer.Serialize(new{strength=3}),                          Price=15,   Description="Lame ébréchée à moitié, mais encore dangereuse" },
            new Item { Name="Épée Courte en Fer",      Type=Constants.ItemTypeWeapon, EffectValue=10, StatModifiers=JsonSerializer.Serialize(new{strength=7}),                          Price=80,   Description="Lame équilibrée pour les combattants débutants" },
            new Item { Name="Arc Long Elfique",        Type=Constants.ItemTypeWeapon, EffectValue=15, StatModifiers=JsonSerializer.Serialize(new{strength=8,speed=5}),                  Price=220,  Description="Arc en if enchanté dont les flèches semblent chercher leur cible" },
            new Item { Name="Épée Longue en Acier",    Type=Constants.ItemTypeWeapon, EffectValue=22, StatModifiers=JsonSerializer.Serialize(new{strength=14}),                         Price=380,  Description="Lame forgée par les maîtres artisans du Nord" },
            new Item { Name="Bâton des Arcanes",       Type=Constants.ItemTypeWeapon, EffectValue=25, StatModifiers=JsonSerializer.Serialize(new{intelligence=18,maxMP=40}),            Price=450,  Description="Focalisateur taillé dans du bois de sorbier lunaire" },
            new Item { Name="Hache de Guerre",         Type=Constants.ItemTypeWeapon, EffectValue=30, StatModifiers=JsonSerializer.Serialize(new{strength=22,speed=-3}),                Price=480,  Description="Arme brutale sacrifiant la mobilité pour une puissance dévastatrice" },
            new Item { Name="Poignards Jumeaux",       Type=Constants.ItemTypeWeapon, EffectValue=28, StatModifiers=JsonSerializer.Serialize(new{strength=9,speed=9}),                  Price=420,  Description="Paire de lames conçue pour les attaques rapides en succession" },
            new Item { Name="Lame de Lumière Sacrée",  Type=Constants.ItemTypeWeapon, EffectValue=40, StatModifiers=JsonSerializer.Serialize(new{strength=18,intelligence=8}),          Price=750,  Description="Épée bénie capable de dissoudre les non-morts au contact" },
            new Item { Name="Grimoire du Vide",        Type=Constants.ItemTypeWeapon, EffectValue=50, StatModifiers=JsonSerializer.Serialize(new{intelligence=30,maxMP=60}),            Price=1200, Description="Tome interdit contenant des sorts oubliés depuis des siècles" },
            new Item { Name="Épée Runique Ancestrale", Type=Constants.ItemTypeWeapon, EffectValue=65, StatModifiers=JsonSerializer.Serialize(new{strength=28,intelligence=12}),         Price=1800, Description="Lame gravée de runes canalisant la magie vers son fil" },

            // ── Armures (10) ──────────────────────────────────────
            new Item { Name="Guenilles Renforcées",    Type=Constants.ItemTypeArmor,  EffectValue=5,  StatModifiers=JsonSerializer.Serialize(new{maxHP=10}),                            Price=20,   Description="Tissu grossier offrant une protection dérisoire mais suffisante au départ" },
            new Item { Name="Vêtements en Cuir",       Type=Constants.ItemTypeArmor,  EffectValue=10, StatModifiers=JsonSerializer.Serialize(new{maxHP=18}),                            Price=75,   Description="Protection légère idéale pour les voleurs et les archers" },
            new Item { Name="Tunique de Mage",         Type=Constants.ItemTypeArmor,  EffectValue=12, StatModifiers=JsonSerializer.Serialize(new{maxHP=10,maxMP=25}),                   Price=120,  Description="Tissu enchanté amplifiant les capacités magiques" },
            new Item { Name="Gambison d'Écuyer",       Type=Constants.ItemTypeArmor,  EffectValue=18, StatModifiers=JsonSerializer.Serialize(new{maxHP=30}),                            Price=200,  Description="Protection matelassée absorbant les coups sans gêner les mouvements" },
            new Item { Name="Cotte de Mailles",        Type=Constants.ItemTypeArmor,  EffectValue=25, StatModifiers=JsonSerializer.Serialize(new{maxHP=50}),                            Price=380,  Description="Protection intermédiaire offrant mobilité et résistance" },
            new Item { Name="Plastron d'Acier",        Type=Constants.ItemTypeArmor,  EffectValue=35, StatModifiers=JsonSerializer.Serialize(new{maxHP=75,speed=-2}),                   Price=620,  Description="Armure lourde forgée pour les guerriers de première ligne" },
            new Item { Name="Robe des Arcanes",        Type=Constants.ItemTypeArmor,  EffectValue=30, StatModifiers=JsonSerializer.Serialize(new{maxHP=30,maxMP=60,intelligence=8}),    Price=580,  Description="Vêtement magique tissé avec des fils d'éther pur" },
            new Item { Name="Armure Runique",          Type=Constants.ItemTypeArmor,  EffectValue=45, StatModifiers=JsonSerializer.Serialize(new{maxHP=90,intelligence=5}),             Price=950,  Description="Armure couverte de runes protectrices à la fois physiques et magiques" },
            new Item { Name="Armure du Crépuscule",    Type=Constants.ItemTypeArmor,  EffectValue=55, StatModifiers=JsonSerializer.Serialize(new{maxHP=110,maxMP=35}),                  Price=1300, Description="Alliage rare absorbant coups physiques et magiques simultanément" },
            new Item { Name="Égide du Titan",          Type=Constants.ItemTypeArmor,  EffectValue=70, StatModifiers=JsonSerializer.Serialize(new{maxHP=150,speed=-3}),                  Price=2000, Description="Armure légendaire forgée dans le métal d'une étoile tombée" },

            // ── Accessoires (10) ──────────────────────────────────
            new Item { Name="Bague Ordinaire",         Type=Constants.ItemTypeAccessory, EffectValue=3,  StatModifiers=JsonSerializer.Serialize(new{strength=2}),                       Price=30,   Description="Simple anneau en cuivre offrant un léger avantage au combat" },
            new Item { Name="Amulette en Os",          Type=Constants.ItemTypeAccessory, EffectValue=5,  StatModifiers=JsonSerializer.Serialize(new{maxHP=8}),                          Price=50,   Description="Talisman primitif taillé dans l'os d'un prédateur vaincu" },
            new Item { Name="Anneau de Force",         Type=Constants.ItemTypeAccessory, EffectValue=12, StatModifiers=JsonSerializer.Serialize(new{strength=6}),                       Price=180,  Description="Anneau forgé avec du métal des profondeurs, augmentant la puissance" },
            new Item { Name="Amulette d'Esprit",       Type=Constants.ItemTypeAccessory, EffectValue=15, StatModifiers=JsonSerializer.Serialize(new{intelligence=8,maxMP=20}),          Price=280,  Description="Pendentif en cristal affinant la perception magique" },
            new Item { Name="Bottes de Mercure",       Type=Constants.ItemTypeAccessory, EffectValue=14, StatModifiers=JsonSerializer.Serialize(new{speed=10}),                         Price=250,  Description="Chaussures enchantées par un esprit du vent" },
            new Item { Name="Ceinture du Survivant",   Type=Constants.ItemTypeAccessory, EffectValue=18, StatModifiers=JsonSerializer.Serialize(new{maxHP=35}),                         Price=220,  Description="Accessoire tressé avec des fibres régénératrices" },
            new Item { Name="Cape de l'Ombre",         Type=Constants.ItemTypeAccessory, EffectValue=20, StatModifiers=JsonSerializer.Serialize(new{speed=7,strength=5}),               Price=480,  Description="Cape tissée de fils d'obscurité, rend son porteur difficile à percevoir" },
            new Item { Name="Bague du Savant",         Type=Constants.ItemTypeAccessory, EffectValue=25, StatModifiers=JsonSerializer.Serialize(new{intelligence=14,maxMP=30}),         Price=600,  Description="Anneau d'un mage défunt, encore imprégné de sa puissance" },
            new Item { Name="Talisman du Dragon",      Type=Constants.ItemTypeAccessory, EffectValue=38, StatModifiers=JsonSerializer.Serialize(new{strength=12,intelligence=10,maxHP=20}),Price=1100,Description="Écaille enchantée d'un dragon mort offrant protection multiple" },
            new Item { Name="Couronne de l'Aube",      Type=Constants.ItemTypeAccessory, EffectValue=55, StatModifiers=JsonSerializer.Serialize(new{intelligence=20,strength=10,maxMP=50,speed=5}),Price=2200,Description="Diadème légendaire amplifiant tous les aspects de son porteur" },

            // ── Consommables HP (10) ──────────────────────────────
            new Item { Name="Herbe Cicatrisante",      Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionHp, EffectValue=20,  Price=10,  Description="Plante médicinale commune stoppant les saignements" },
            new Item { Name="Bandage Enchanté",        Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionHp, EffectValue=35,  Price=18,  Description="Tissu imprégné d'herbes régénératrices" },
            new Item { Name="Fiole de Soin",           Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionHp, EffectValue=60,  Price=30,  Description="Décoction alchimique réparant les tissus endommagés" },
            new Item { Name="Potion de Guérison",      Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionHp, EffectValue=100, Price=55,  Description="Formule standard restauratrice d'une grande partie des forces" },
            new Item { Name="Potion de Vitalité",      Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionHp, EffectValue=150, Price=80,  Description="Concentré alchimique réparant les blessures graves" },
            new Item { Name="Elixir de Sève",          Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionHp, EffectValue=200, Price=120, Description="Sève d'arbre millénaire aux propriétés régénératrices extraordinaires" },
            new Item { Name="Fiole du Phoenix",        Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionHp, EffectValue=280, Price=200, Description="Larme d'un phénix capturée, régénère les blessures profondes" },
            new Item { Name="Élixir de Guérison",      Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionHp, EffectValue=350, Price=300, Description="Préparation rare closant toutes les blessures en un instant" },
            new Item { Name="Ambroisie Mineure",       Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionHp, EffectValue=500, Price=500, Description="Nectar divin offrant une régénération quasi complète" },
            new Item { Name="Eau de Jouvence",         Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionHp, EffectValue=999, Price=1000,Description="Eau d'une source légendaire restaurant intégralement les forces vitales" },

            // ── Consommables MP (10) ──────────────────────────────
            new Item { Name="Éclat de Cristal",        Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionMp, EffectValue=15,  Price=12,  Description="Fragment libérant une étincelle d'énergie magique" },
            new Item { Name="Poudre de Mana",          Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionMp, EffectValue=30,  Price=22,  Description="Poudre translucide se dissolvant dans la bouche, restaurant le mana" },
            new Item { Name="Cristal de Mana",         Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionMp, EffectValue=50,  Price=35,  Description="Fragment cristallin libérant de l'énergie magique pure" },
            new Item { Name="Essence Magique",         Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionMp, EffectValue=80,  Price=55,  Description="Liquide luminescent distillé d'éthers magiques condensés" },
            new Item { Name="Potion d'Éther",          Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionMp, EffectValue=120, Price=85,  Description="Potion distillée d'éther pur restaurant rapidement le mana" },
            new Item { Name="Fiole Astrale",           Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionMp, EffectValue=180, Price=140, Description="Liquide capturant l'essence des étoiles, source de mana inépuisable" },
            new Item { Name="Larme de Mage",           Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionMp, EffectValue=250, Price=220, Description="Larme cristallisée d'un mage légendaire, concentrée en énergie pure" },
            new Item { Name="Essence du Vide",         Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionMp, EffectValue=350, Price=380, Description="Extrait du vide inter-dimensionnel, recharge instantanément le mana" },
            new Item { Name="Philtre de Puissance",    Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionMp, EffectValue=500, Price=600, Description="Formule alchimique ultime amplifiant temporairement les capacités magiques" },
            new Item { Name="Nectar des Dieux",        Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryPotionMp, EffectValue=999, Price=1200,Description="Breuvage divin restaurant intégralement le mana et augmentant temporairement la magie" },

            // ── Élixirs (5) ───────────────────────────────────────
            new Item { Name="Élixir de Puissance",     Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryElixir, EffectValue=200, Price=300, Description="Double temporairement la force et l'intelligence" },
            new Item { Name="Sérum de Rapidité",       Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryElixir, EffectValue=200, Price=320, Description="Triple momentanément la vitesse de déplacement et d'attaque" },
            new Item { Name="Philtre de l'Invincible", Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryElixir, EffectValue=500, Price=600, Description="Offre une immunité temporaire aux dégâts physiques" },
            new Item { Name="Élixir du Dragon",        Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryElixir, EffectValue=700, Price=900, Description="Confère temporairement la résistance élémentaire d'un dragon" },
            new Item { Name="Panacée Légendaire",      Type=Constants.ItemTypeConsumable, Category=Constants.ItemCategoryElixir, EffectValue=999, Price=1500,Description="Remède universel restaurant HP et MP, quasi introuvable" },
        };
        await context.Items.AddRangeAsync(items);
        await context.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════
        // 5. SKILLS (18)
        // ═══════════════════════════════════════════════════════════
        var skills = new[]
        {
            new Skill { Name="Boule de Feu",       MPCost=15, BaseDamage=35,  EffectType=Constants.EffectDamage, ElementType=Constants.ElementFire,      Description="Projectile enflammé d'intensité modérée" },
            new Skill { Name="Foudre",             MPCost=18, BaseDamage=45,  EffectType=Constants.EffectDamage, ElementType=Constants.ElementLightning, Description="Éclair direct frappant avec une précision absolue" },
            new Skill { Name="Blizzard",           MPCost=30, BaseDamage=60,  EffectType=Constants.EffectDamage, ElementType=Constants.ElementIce,       Description="Tempête glaciale ralentissant et blessant les ennemis" },
            new Skill { Name="Météore",            MPCost=45, BaseDamage=95,  EffectType=Constants.EffectDamage, ElementType=Constants.ElementFire,      Description="Rocher céleste embrasé à la puissance dévastatrice" },
            new Skill { Name="Lance Sacrée",       MPCost=25, BaseDamage=55,  EffectType=Constants.EffectDamage, ElementType=Constants.ElementNeutral,   Description="Projectile de lumière efficace contre les morts-vivants" },
            new Skill { Name="Lame du Vent",       MPCost=12, BaseDamage=28,  EffectType=Constants.EffectDamage, ElementType=Constants.ElementNeutral,   Description="Tranchant invisible propulsé à grande vitesse" },
            new Skill { Name="Nova de Ténèbres",   MPCost=55, BaseDamage=110, EffectType=Constants.EffectDamage, ElementType=Constants.ElementNeutral,   Description="Explosion d'énergie sombre ravageant tout ce qu'elle touche" },
            new Skill { Name="Soin",               MPCost=10, HealAmount=50,  EffectType=Constants.EffectHeal,   ElementType=Constants.ElementNeutral,   Description="Incantation basique restaurant les points de vie" },
            new Skill { Name="Soin Mineur",        MPCost=6,  HealAmount=25,  EffectType=Constants.EffectHeal,   ElementType=Constants.ElementNeutral,   Description="Formule rapide pour les petites blessures" },
            new Skill { Name="Régénération",       MPCost=20, HealAmount=80,  EffectType=Constants.EffectHeal,   ElementType=Constants.ElementNeutral,   Description="Accélère la cicatrisation sur plusieurs secondes" },
            new Skill { Name="Soin de Groupe",     MPCost=35, HealAmount=60,  EffectType=Constants.EffectHeal,   ElementType=Constants.ElementNeutral,   Description="Aura curative soignant tous les alliés proches" },
            new Skill { Name="Résurrection",       MPCost=80, HealAmount=200, EffectType=Constants.EffectHeal,   ElementType=Constants.ElementNeutral,   Description="Sort ultime rappelant un allié à la vie" },
            new Skill { Name="Rage Berserker",     MPCost=20, EffectType=Constants.EffectBuff,   ElementType=Constants.ElementNeutral,   Description="Décuple la force physique au prix de la défense" },
            new Skill { Name="Hâte",               MPCost=15, EffectType=Constants.EffectBuff,   ElementType=Constants.ElementNeutral,   Description="Accélère les mouvements et les attaques" },
            new Skill { Name="Bouclier Magique",   MPCost=25, EffectType=Constants.EffectBuff,   ElementType=Constants.ElementNeutral,   Description="Barrière absorbant les prochains dégâts magiques" },
            new Skill { Name="Bénédiction",        MPCost=30, EffectType=Constants.EffectBuff,   ElementType=Constants.ElementNeutral,   Description="Augmente temporairement toutes les statistiques" },
            new Skill { Name="Malédiction",        MPCost=22, EffectType=Constants.EffectDebuff, ElementType=Constants.ElementNeutral,   Description="Réduit la résistance de l'ennemi à toutes les formes de dégâts" },
            new Skill { Name="Ralentissement",     MPCost=14, EffectType=Constants.EffectDebuff, ElementType=Constants.ElementIce,       Description="Congèle partiellement les membres, réduisant la vitesse ennemie" },
        };
        await context.Skills.AddRangeAsync(skills);
        await context.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════
        // 6. COMBAT STATS — variés pour alimenter le leaderboard
        // ═══════════════════════════════════════════════════════════
        var combatStats = new[]
        {
            new CombatStats { PlayerId=profiles[0].Id,  TotalCombats=85,   CombatsWon=68,   CombatsLost=17,  TotalDamageDealt=42000,   TotalDamageTaken=18000,  TotalPlaytimeMinutes=620  },
            new CombatStats { PlayerId=profiles[1].Id,  TotalCombats=220,  CombatsWon=195,  CombatsLost=25,  TotalDamageDealt=188000,  TotalDamageTaken=55000,  TotalPlaytimeMinutes=2100 },
            new CombatStats { PlayerId=profiles[2].Id,  TotalCombats=32,   CombatsWon=22,   CombatsLost=10,  TotalDamageDealt=8500,    TotalDamageTaken=4200,   TotalPlaytimeMinutes=180  },
            new CombatStats { PlayerId=profiles[3].Id,  TotalCombats=480,  CombatsWon=450,  CombatsLost=30,  TotalDamageDealt=520000,  TotalDamageTaken=98000,  TotalPlaytimeMinutes=5800 },
            new CombatStats { PlayerId=profiles[4].Id,  TotalCombats=3,    CombatsWon=1,    CombatsLost=2,   TotalDamageDealt=180,     TotalDamageTaken=420,    TotalPlaytimeMinutes=12   },
            new CombatStats { PlayerId=profiles[5].Id,  TotalCombats=360,  CombatsWon=328,  CombatsLost=32,  TotalDamageDealt=398000,  TotalDamageTaken=82000,  TotalPlaytimeMinutes=4200 },
            new CombatStats { PlayerId=profiles[6].Id,  TotalCombats=1200, CombatsWon=1155, CombatsLost=45,  TotalDamageDealt=1850000, TotalDamageTaken=180000, TotalPlaytimeMinutes=18000},
            new CombatStats { PlayerId=profiles[7].Id,  TotalCombats=55,   CombatsWon=40,   CombatsLost=15,  TotalDamageDealt=18000,   TotalDamageTaken=8500,   TotalPlaytimeMinutes=380  },
            new CombatStats { PlayerId=profiles[8].Id,  TotalCombats=160,  CombatsWon=130,  CombatsLost=30,  TotalDamageDealt=95000,   TotalDamageTaken=38000,  TotalPlaytimeMinutes=1400 },
            new CombatStats { PlayerId=profiles[9].Id,  TotalCombats=780,  CombatsWon=740,  CombatsLost=40,  TotalDamageDealt=780000,  TotalDamageTaken=120000, TotalPlaytimeMinutes=9200 },
            new CombatStats { PlayerId=profiles[10].Id, TotalCombats=10,   CombatsWon=6,    CombatsLost=4,   TotalDamageDealt=680,     TotalDamageTaken=900,    TotalPlaytimeMinutes=45   },
            new CombatStats { PlayerId=profiles[11].Id, TotalCombats=9999, CombatsWon=9990, CombatsLost=9,   TotalDamageDealt=9999999, TotalDamageTaken=100000, TotalPlaytimeMinutes=99999},
            new CombatStats { PlayerId=profiles[12].Id, TotalCombats=110,  CombatsWon=88,   CombatsLost=22,  TotalDamageDealt=58000,   TotalDamageTaken=22000,  TotalPlaytimeMinutes=780  },
            new CombatStats { PlayerId=profiles[13].Id, TotalCombats=290,  CombatsWon=255,  CombatsLost=35,  TotalDamageDealt=245000,  TotalDamageTaken=68000,  TotalPlaytimeMinutes=3200 },
            new CombatStats { PlayerId=profiles[14].Id, TotalCombats=8,    CombatsWon=4,    CombatsLost=4,   TotalDamageDealt=520,     TotalDamageTaken=680,    TotalPlaytimeMinutes=35   },
            new CombatStats { PlayerId=profiles[15].Id, TotalCombats=920,  CombatsWon=880,  CombatsLost=40,  TotalDamageDealt=920000,  TotalDamageTaken=150000, TotalPlaytimeMinutes=11000},
            new CombatStats { PlayerId=profiles[16].Id, TotalCombats=92,   CombatsWon=72,   CombatsLost=20,  TotalDamageDealt=48000,   TotalDamageTaken=19000,  TotalPlaytimeMinutes=520  },
            new CombatStats { PlayerId=profiles[17].Id, TotalCombats=185,  CombatsWon=155,  CombatsLost=30,  TotalDamageDealt=138000,  TotalDamageTaken=48000,  TotalPlaytimeMinutes=1800 },
            new CombatStats { PlayerId=profiles[18].Id, TotalCombats=420,  CombatsWon=390,  CombatsLost=30,  TotalDamageDealt=458000,  TotalDamageTaken=88000,  TotalPlaytimeMinutes=5200 },
            new CombatStats { PlayerId=profiles[19].Id, TotalCombats=38,   CombatsWon=26,   CombatsLost=12,  TotalDamageDealt=9800,    TotalDamageTaken=5200,   TotalPlaytimeMinutes=260  },
        };
        await context.CombatStats.AddRangeAsync(combatStats);
        await context.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════
        // 7. PLAYER INVENTORY — équipements typés par personnage
        // ═══════════════════════════════════════════════════════════
        // items[]: 0-9 armes, 10-19 armures, 20-29 accessoires,
        //          30-39 conso_hp, 40-49 conso_mp, 50-54 élixirs
        var inventories = new List<PlayerInventory>
        {
            // Aelindra (mage niv.15)
            new() { PlayerId=profiles[0].Id, ItemId=items[4].Id,  Quantity=1,  IsEquipped=true  }, // Bâton des Arcanes
            new() { PlayerId=profiles[0].Id, ItemId=items[12].Id, Quantity=1,  IsEquipped=true  }, // Tunique de Mage
            new() { PlayerId=profiles[0].Id, ItemId=items[23].Id, Quantity=1,  IsEquipped=true  }, // Amulette d'Esprit
            new() { PlayerId=profiles[0].Id, ItemId=items[33].Id, Quantity=5,  IsEquipped=false }, // Fiole de Soin
            new() { PlayerId=profiles[0].Id, ItemId=items[42].Id, Quantity=8,  IsEquipped=false }, // Cristal de Mana
            // Borak (guerrier niv.32)
            new() { PlayerId=profiles[1].Id, ItemId=items[6].Id,  Quantity=1,  IsEquipped=true  }, // Hache de Guerre
            new() { PlayerId=profiles[1].Id, ItemId=items[15].Id, Quantity=1,  IsEquipped=true  }, // Plastron d'Acier
            new() { PlayerId=profiles[1].Id, ItemId=items[22].Id, Quantity=1,  IsEquipped=true  }, // Anneau de Force
            new() { PlayerId=profiles[1].Id, ItemId=items[25].Id, Quantity=1,  IsEquipped=true  }, // Ceinture du Survivant
            new() { PlayerId=profiles[1].Id, ItemId=items[35].Id, Quantity=10, IsEquipped=false }, // Potion de Guérison
            // Cyraelle (archère niv.7)
            new() { PlayerId=profiles[2].Id, ItemId=items[2].Id,  Quantity=1,  IsEquipped=true  }, // Arc Long Elfique
            new() { PlayerId=profiles[2].Id, ItemId=items[11].Id, Quantity=1,  IsEquipped=true  }, // Vêtements en Cuir
            new() { PlayerId=profiles[2].Id, ItemId=items[31].Id, Quantity=3,  IsEquipped=false }, // Bandage Enchanté
            // Daevar (paladin niv.55)
            new() { PlayerId=profiles[3].Id, ItemId=items[7].Id,  Quantity=1,  IsEquipped=true  }, // Lame de Lumière
            new() { PlayerId=profiles[3].Id, ItemId=items[17].Id, Quantity=1,  IsEquipped=true  }, // Armure Runique
            new() { PlayerId=profiles[3].Id, ItemId=items[28].Id, Quantity=1,  IsEquipped=true  }, // Talisman du Dragon
            new() { PlayerId=profiles[3].Id, ItemId=items[37].Id, Quantity=8,  IsEquipped=false }, // Fiole du Phoenix
            new() { PlayerId=profiles[3].Id, ItemId=items[46].Id, Quantity=5,  IsEquipped=false }, // Larme de Mage
            // Elowen (débutant niv.1)
            new() { PlayerId=profiles[4].Id, ItemId=items[0].Id,  Quantity=1,  IsEquipped=true  }, // Couteau Brisé
            new() { PlayerId=profiles[4].Id, ItemId=items[10].Id, Quantity=1,  IsEquipped=true  }, // Guenilles
            new() { PlayerId=profiles[4].Id, ItemId=items[30].Id, Quantity=2,  IsEquipped=false }, // Herbe Cicatrisante
            // Gryphael (grand mage niv.88)
            new() { PlayerId=profiles[6].Id, ItemId=items[8].Id,  Quantity=1,  IsEquipped=true  }, // Grimoire du Vide
            new() { PlayerId=profiles[6].Id, ItemId=items[18].Id, Quantity=1,  IsEquipped=true  }, // Armure du Crépuscule
            new() { PlayerId=profiles[6].Id, ItemId=items[29].Id, Quantity=1,  IsEquipped=true  }, // Couronne de l'Aube
            new() { PlayerId=profiles[6].Id, ItemId=items[49].Id, Quantity=5,  IsEquipped=false }, // Nectar des Dieux
            new() { PlayerId=profiles[6].Id, ItemId=items[53].Id, Quantity=3,  IsEquipped=false }, // Élixir du Dragon
            // Jorath (tank niv.67)
            new() { PlayerId=profiles[9].Id, ItemId=items[5].Id,  Quantity=1,  IsEquipped=true  }, // Hache de Guerre
            new() { PlayerId=profiles[9].Id, ItemId=items[19].Id, Quantity=1,  IsEquipped=true  }, // Égide du Titan
            new() { PlayerId=profiles[9].Id, ItemId=items[22].Id, Quantity=1,  IsEquipped=true  }, // Anneau de Force
            new() { PlayerId=profiles[9].Id, ItemId=items[25].Id, Quantity=1,  IsEquipped=true  }, // Ceinture du Survivant
        };
        await context.PlayerInventory.AddRangeAsync(inventories);
        await context.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════
        // 8. PLAYER SKILLS
        // ═══════════════════════════════════════════════════════════
        var playerSkills = new List<PlayerSkill>
        {
            // Aelindra (mage)
            new() { PlayerId=profiles[0].Id, SkillId=skills[0].Id,  UnlockedAt=DateTime.UtcNow.AddDays(-30) },
            new() { PlayerId=profiles[0].Id, SkillId=skills[1].Id,  UnlockedAt=DateTime.UtcNow.AddDays(-20) },
            new() { PlayerId=profiles[0].Id, SkillId=skills[7].Id,  UnlockedAt=DateTime.UtcNow.AddDays(-25) },
            new() { PlayerId=profiles[0].Id, SkillId=skills[13].Id, UnlockedAt=DateTime.UtcNow.AddDays(-10) },
            // Borak (guerrier)
            new() { PlayerId=profiles[1].Id, SkillId=skills[12].Id, UnlockedAt=DateTime.UtcNow.AddDays(-60) },
            new() { PlayerId=profiles[1].Id, SkillId=skills[13].Id, UnlockedAt=DateTime.UtcNow.AddDays(-50) },
            new() { PlayerId=profiles[1].Id, SkillId=skills[7].Id,  UnlockedAt=DateTime.UtcNow.AddDays(-55) },
            // Gryphael (grand mage)
            new() { PlayerId=profiles[6].Id, SkillId=skills[0].Id,  UnlockedAt=DateTime.UtcNow.AddDays(-300) },
            new() { PlayerId=profiles[6].Id, SkillId=skills[1].Id,  UnlockedAt=DateTime.UtcNow.AddDays(-280) },
            new() { PlayerId=profiles[6].Id, SkillId=skills[2].Id,  UnlockedAt=DateTime.UtcNow.AddDays(-260) },
            new() { PlayerId=profiles[6].Id, SkillId=skills[3].Id,  UnlockedAt=DateTime.UtcNow.AddDays(-240) },
            new() { PlayerId=profiles[6].Id, SkillId=skills[4].Id,  UnlockedAt=DateTime.UtcNow.AddDays(-220) },
            new() { PlayerId=profiles[6].Id, SkillId=skills[6].Id,  UnlockedAt=DateTime.UtcNow.AddDays(-200) },
            new() { PlayerId=profiles[6].Id, SkillId=skills[7].Id,  UnlockedAt=DateTime.UtcNow.AddDays(-290) },
            new() { PlayerId=profiles[6].Id, SkillId=skills[9].Id,  UnlockedAt=DateTime.UtcNow.AddDays(-180) },
            new() { PlayerId=profiles[6].Id, SkillId=skills[11].Id, UnlockedAt=DateTime.UtcNow.AddDays(-100) },
            new() { PlayerId=profiles[6].Id, SkillId=skills[15].Id, UnlockedAt=DateTime.UtcNow.AddDays(-150) },
            new() { PlayerId=profiles[6].Id, SkillId=skills[16].Id, UnlockedAt=DateTime.UtcNow.AddDays(-120) },
            // Daevar (paladin)
            new() { PlayerId=profiles[3].Id, SkillId=skills[4].Id,  UnlockedAt=DateTime.UtcNow.AddDays(-150) },
            new() { PlayerId=profiles[3].Id, SkillId=skills[7].Id,  UnlockedAt=DateTime.UtcNow.AddDays(-160) },
            new() { PlayerId=profiles[3].Id, SkillId=skills[9].Id,  UnlockedAt=DateTime.UtcNow.AddDays(-140) },
            new() { PlayerId=profiles[3].Id, SkillId=skills[14].Id, UnlockedAt=DateTime.UtcNow.AddDays(-100) },
            new() { PlayerId=profiles[3].Id, SkillId=skills[15].Id, UnlockedAt=DateTime.UtcNow.AddDays(-80)  },
        };
        await context.PlayerSkills.AddRangeAsync(playerSkills);
        await context.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════
        // 9. BESTIARY UNLOCKS — ennemis débloqés par les joueurs
        // ═══════════════════════════════════════════════════════════
        var bestiaryUnlocks = new List<BestiaryUnlock>();

        // Elowen (niv.1) — 2 ennemis
        bestiaryUnlocks.Add(new() { PlayerId = profiles[4].Id, EnemyId = enemies[0].Id, UnlockedAt = DateTime.UtcNow.AddDays(-2) });
        bestiaryUnlocks.Add(new() { PlayerId = profiles[4].Id, EnemyId = enemies[1].Id, UnlockedAt = DateTime.UtcNow.AddDays(-1) });

        // Aelindra (niv.15) — 15 ennemis
        for (int i = 0; i < 15; i++)
            bestiaryUnlocks.Add(new() { PlayerId = profiles[0].Id, EnemyId = enemies[i].Id, UnlockedAt = DateTime.UtcNow.AddDays(-(15 - i) * 4) });

        // Borak (niv.32) — 30 ennemis
        for (int i = 0; i < 30; i++)
            bestiaryUnlocks.Add(new() { PlayerId = profiles[1].Id, EnemyId = enemies[i].Id, UnlockedAt = DateTime.UtcNow.AddDays(-(30 - i) * 3) });

        // Daevar (niv.55) — 50 ennemis
        for (int i = 0; i < 50; i++)
            bestiaryUnlocks.Add(new() { PlayerId = profiles[3].Id, EnemyId = enemies[i].Id, UnlockedAt = DateTime.UtcNow.AddDays(-(50 - i) * 4) });

        // Gryphael (niv.88) — tous les ennemis
        for (int i = 0; i < 70; i++)
            bestiaryUnlocks.Add(new() { PlayerId = profiles[6].Id, EnemyId = enemies[i].Id, UnlockedAt = DateTime.UtcNow.AddDays(-(70 - i) * 5) });

        // Jorath (niv.67) — 60 ennemis
        for (int i = 0; i < 60; i++)
            bestiaryUnlocks.Add(new() { PlayerId = profiles[9].Id, EnemyId = enemies[i].Id, UnlockedAt = DateTime.UtcNow.AddDays(-(60 - i) * 5) });

        await context.BestiaryUnlocks.AddRangeAsync(bestiaryUnlocks);
        await context.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════
        // 10. GAME SAVES
        // ═══════════════════════════════════════════════════════════
        var gameSaves = new[]
        {
            new GameSave { PlayerId=profiles[0].Id,  CurrentZone="Dev Island", PositionX=0.1f, PositionY=0.3f,  SavedAt=DateTime.UtcNow.AddHours(-2),  InventoryData=JsonSerializer.Serialize(new{usedSlots=6}),  QuestFlags=JsonSerializer.Serialize(new{tutorial=true}) },
            new GameSave { PlayerId=profiles[1].Id,  CurrentZone="Dev Island", PositionX=0.5f, PositionY=0.5f,  SavedAt=DateTime.UtcNow.AddHours(-5),  InventoryData=JsonSerializer.Serialize(new{usedSlots=9}),  QuestFlags=JsonSerializer.Serialize(new{tutorial=true}) },
            new GameSave { PlayerId=profiles[3].Id,  CurrentZone="Dev Island", PositionX=0.8f, PositionY=0.2f,  SavedAt=DateTime.UtcNow.AddHours(-1),  InventoryData=JsonSerializer.Serialize(new{usedSlots=18}), QuestFlags=JsonSerializer.Serialize(new{tutorial=true,boss1=true}) },
            new GameSave { PlayerId=profiles[4].Id,  CurrentZone="Dev Island", PositionX=0.0f, PositionY=0.0f,  SavedAt=DateTime.UtcNow.AddHours(-20), InventoryData=JsonSerializer.Serialize(new{usedSlots=3}),  QuestFlags=JsonSerializer.Serialize(new{tutorial=false}) },
            new GameSave { PlayerId=profiles[6].Id,  CurrentZone="Dev Island", PositionX=0.9f, PositionY=0.9f,  SavedAt=DateTime.UtcNow.AddMinutes(-30),InventoryData=JsonSerializer.Serialize(new{usedSlots=25}), QuestFlags=JsonSerializer.Serialize(new{tutorial=true,allBosses=true}) },
            new GameSave { PlayerId=profiles[9].Id,  CurrentZone="Dev Island", PositionX=0.7f, PositionY=0.8f,  SavedAt=DateTime.UtcNow.AddHours(-4),  InventoryData=JsonSerializer.Serialize(new{usedSlots=20}), QuestFlags=JsonSerializer.Serialize(new{tutorial=true,boss1=true,boss2=true}) },
            new GameSave { PlayerId=profiles[11].Id, CurrentZone="Dev Island", PositionX=0.5f, PositionY=0.5f,  SavedAt=DateTime.UtcNow.AddMinutes(-10),InventoryData=JsonSerializer.Serialize(new{usedSlots=40}), QuestFlags=JsonSerializer.Serialize(new{tutorial=true,allQuests=true}) },
        };
        await context.GameSaves.AddRangeAsync(gameSaves);
        await context.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════
        // 11. USER CONSENTS
        // ═══════════════════════════════════════════════════════════
        var consents = users.Select((u, i) => new UserConsent
        {
            UserId = u.Id,
            AnalyticsConsent = i % 3 != 2,
            MarketingConsent = i % 4 == 0,
        }).ToArray();
        await context.UserConsents.AddRangeAsync(consents);
        await context.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════
        // 12. AUDIT LOGS
        // ═══════════════════════════════════════════════════════════
        var auditLogs = new[]
        {
            new AuditLog { UserId=users[0].Id,  EventType="LOGIN_SUCCESS", EventData=JsonSerializer.Serialize(new{method="password"}), IpAddress=Constants.SeedIpAddress, UserAgent=Constants.SeedUserAgent, Timestamp=DateTime.UtcNow.AddHours(-2)  },
            new AuditLog { UserId=users[11].Id, EventType="LOGIN_SUCCESS", EventData=JsonSerializer.Serialize(new{method="mfa"}),      IpAddress=Constants.SeedIpAddress, UserAgent=Constants.SeedUserAgent, Timestamp=DateTime.UtcNow.AddMinutes(-10)},
            new AuditLog { UserId=users[6].Id,  EventType="LOGIN_SUCCESS", EventData=JsonSerializer.Serialize(new{method="mfa"}),      IpAddress=Constants.SeedIpAddress, UserAgent=Constants.SeedUserAgent, Timestamp=DateTime.UtcNow.AddMinutes(-30)},
            new AuditLog { UserId=users[3].Id,  EventType="DATA_EXPORT",   EventData=JsonSerializer.Serialize(new{format="json"}),    IpAddress=Constants.SeedIpAddress, UserAgent=Constants.SeedUserAgent, Timestamp=DateTime.UtcNow.AddHours(-1)   },
            new AuditLog { UserId=users[11].Id, EventType="ADMIN_ACTION",  EventData=JsonSerializer.Serialize(new{action="seed"}),     IpAddress=Constants.SeedIpAddress, UserAgent=Constants.SeedUserAgent, Timestamp=DateTime.UtcNow.AddMinutes(-5) },
        };
        await context.AuditLogs.AddRangeAsync(auditLogs);
        await context.SaveChangesAsync();
    }

    private static string GetRequiredSeedPassword(IConfiguration? configuration, string key)
    {
        var value = configuration?[key];
        if (string.IsNullOrWhiteSpace(value))
            throw new InvalidOperationException(
                $"La variable de seed '{key}' est requise. Configurez-la dans les variables d'environnement.");
        return value;
    }
}