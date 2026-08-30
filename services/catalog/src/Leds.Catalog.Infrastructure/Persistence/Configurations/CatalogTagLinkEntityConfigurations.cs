using Leds.Catalog.Infrastructure.Persistence.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Leds.Catalog.Infrastructure.Persistence.Configurations;

public sealed class EnemyTagEntityConfiguration : IEntityTypeConfiguration<EnemyTagEntity>
{
    public void Configure(EntityTypeBuilder<EnemyTagEntity> builder)
    {
        builder.ToTable("catalog_enemy_tags");
        builder.HasKey(e => new { e.EnemyDefinitionId, e.TagId });
        builder.Property(e => e.EnemyDefinitionId).HasColumnName("enemy_definition_id");
        builder.Property(e => e.TagId).HasColumnName("tag_id");
        builder.HasOne(e => e.EnemyDefinition).WithMany(e => e.Tags).HasForeignKey(e => e.EnemyDefinitionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Tag).WithMany().HasForeignKey(e => e.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class SkillTagEntityConfiguration : IEntityTypeConfiguration<SkillTagEntity>
{
    public void Configure(EntityTypeBuilder<SkillTagEntity> builder)
    {
        builder.ToTable("catalog_skill_tags");
        builder.HasKey(e => new { e.SkillDefinitionId, e.TagId });
        builder.Property(e => e.SkillDefinitionId).HasColumnName("skill_definition_id");
        builder.Property(e => e.TagId).HasColumnName("tag_id");
        builder.HasOne(e => e.SkillDefinition).WithMany(e => e.Tags).HasForeignKey(e => e.SkillDefinitionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Tag).WithMany().HasForeignKey(e => e.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class ItemTagEntityConfiguration : IEntityTypeConfiguration<ItemTagEntity>
{
    public void Configure(EntityTypeBuilder<ItemTagEntity> builder)
    {
        builder.ToTable("catalog_item_tags");
        builder.HasKey(e => new { e.ItemDefinitionId, e.TagId });
        builder.Property(e => e.ItemDefinitionId).HasColumnName("item_definition_id");
        builder.Property(e => e.TagId).HasColumnName("tag_id");
        builder.HasOne(e => e.ItemDefinition).WithMany(e => e.Tags).HasForeignKey(e => e.ItemDefinitionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Tag).WithMany().HasForeignKey(e => e.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class LawTagEntityConfiguration : IEntityTypeConfiguration<LawTagEntity>
{
    public void Configure(EntityTypeBuilder<LawTagEntity> builder)
    {
        builder.ToTable("catalog_law_tags");
        builder.HasKey(e => new { e.PalaceLawDefinitionId, e.TagId });
        builder.Property(e => e.PalaceLawDefinitionId).HasColumnName("palace_law_definition_id");
        builder.Property(e => e.TagId).HasColumnName("tag_id");
        builder.HasOne(e => e.PalaceLawDefinition).WithMany(e => e.Tags).HasForeignKey(e => e.PalaceLawDefinitionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Tag).WithMany().HasForeignKey(e => e.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class CurseTagEntityConfiguration : IEntityTypeConfiguration<CurseTagEntity>
{
    public void Configure(EntityTypeBuilder<CurseTagEntity> builder)
    {
        builder.ToTable("catalog_curse_tags");
        builder.HasKey(e => new { e.CurseDefinitionId, e.TagId });
        builder.Property(e => e.CurseDefinitionId).HasColumnName("curse_definition_id");
        builder.Property(e => e.TagId).HasColumnName("tag_id");
        builder.HasOne(e => e.CurseDefinition).WithMany(e => e.Tags).HasForeignKey(e => e.CurseDefinitionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Tag).WithMany().HasForeignKey(e => e.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RewardTemplateTagEntityConfiguration : IEntityTypeConfiguration<RewardTemplateTagEntity>
{
    public void Configure(EntityTypeBuilder<RewardTemplateTagEntity> builder)
    {
        builder.ToTable("catalog_reward_tags");
        builder.HasKey(e => new { e.RewardTemplateId, e.TagId });
        builder.Property(e => e.RewardTemplateId).HasColumnName("reward_template_id");
        builder.Property(e => e.TagId).HasColumnName("tag_id");
        builder.HasOne(e => e.RewardTemplate).WithMany(e => e.Tags).HasForeignKey(e => e.RewardTemplateId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Tag).WithMany().HasForeignKey(e => e.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RoomTagEntityConfiguration : IEntityTypeConfiguration<RoomTagEntity>
{
    public void Configure(EntityTypeBuilder<RoomTagEntity> builder)
    {
        builder.ToTable("catalog_room_tags");
        builder.HasKey(e => new { e.RoomDefinitionId, e.TagId });
        builder.Property(e => e.RoomDefinitionId).HasColumnName("room_definition_id");
        builder.Property(e => e.TagId).HasColumnName("tag_id");
        builder.HasOne(e => e.RoomDefinition).WithMany(e => e.Tags).HasForeignKey(e => e.RoomDefinitionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Tag).WithMany().HasForeignKey(e => e.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class RoomBossTagEntityConfiguration : IEntityTypeConfiguration<RoomBossTagEntity>
{
    public void Configure(EntityTypeBuilder<RoomBossTagEntity> builder)
    {
        builder.ToTable("catalog_room_boss_tags");
        builder.HasKey(e => new { e.RoomBossDefinitionId, e.TagId });
        builder.Property(e => e.RoomBossDefinitionId).HasColumnName("room_boss_definition_id");
        builder.Property(e => e.TagId).HasColumnName("tag_id");
        builder.HasOne(e => e.RoomBossDefinition).WithMany(e => e.Tags).HasForeignKey(e => e.RoomBossDefinitionId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(e => e.Tag).WithMany().HasForeignKey(e => e.TagId).OnDelete(DeleteBehavior.Cascade);
    }
}
