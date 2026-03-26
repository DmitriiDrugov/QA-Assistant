using Microsoft.EntityFrameworkCore;
using QA.Backend.Data.Entities;

namespace QA.Backend.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<ConversationEntity> Conversations => Set<ConversationEntity>();
    public DbSet<MessageEntity> Messages => Set<MessageEntity>();
    public DbSet<AiModelSettingsEntity> AiModelSettings => Set<AiModelSettingsEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(entity =>
        {
            entity.ToTable("users");
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Email).IsRequired();
            entity.Property(user => user.Username).IsRequired();
            entity.Property(user => user.PasswordHash).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
        });

        modelBuilder.Entity<ConversationEntity>(entity =>
        {
            entity.ToTable("conversations");
            entity.HasKey(conversation => conversation.Id);
            entity.Property(conversation => conversation.UserId).IsRequired();
            entity.Property(conversation => conversation.Title).IsRequired();

            entity.HasOne(conversation => conversation.User)
                .WithMany(user => user.Conversations)
                .HasForeignKey(conversation => conversation.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<MessageEntity>(entity =>
        {
            entity.ToTable("messages");
            entity.HasKey(message => message.Id);
            entity.Property(message => message.ConversationId).IsRequired();
            entity.Property(message => message.Role).IsRequired();
            entity.Property(message => message.Content).IsRequired();

            entity.HasOne(message => message.Conversation)
                .WithMany(conversation => conversation.Messages)
                .HasForeignKey(message => message.ConversationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AiModelSettingsEntity>(entity =>
        {
            entity.ToTable("ai_model_settings");
            entity.HasKey(settings => settings.Id);
            entity.Property(settings => settings.UserId).IsRequired();
            entity.Property(settings => settings.ModelEndpoint).IsRequired();
            entity.Property(settings => settings.SystemPrompt).IsRequired();
            entity.HasIndex(settings => settings.UserId).IsUnique();

            entity.HasOne(settings => settings.User)
                .WithOne(user => user.AiSettings)
                .HasForeignKey<AiModelSettingsEntity>(settings => settings.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
