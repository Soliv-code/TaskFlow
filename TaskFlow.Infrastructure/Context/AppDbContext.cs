using Microsoft.EntityFrameworkCore;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Infrastructure.Context;

public partial class AppDbContext : DbContext
{
    public AppDbContext()
    {
    }

    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Project> Projects { get; set; }
    public virtual DbSet<ProjectMember> ProjectMembers { get; set; }
    public virtual DbSet<ProjectMemberRole> ProjectMemberRoles { get; set; }
    public virtual DbSet<ProjectTask> ProjectTasks { get; set; }
    public virtual DbSet<ProjectTaskPriority> ProjectTaskPriorities { get; set; }
    public virtual DbSet<ProjectTaskStatus> ProjectTaskStatuses { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<Role> Roles { get; set; }
    public virtual DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Project>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Projects_pkey");

            entity.HasIndex(e => new { e.Name, e.OwnerId }, "ix_projects_name_owner").IsUnique();

            entity.HasIndex(e => e.OwnerId, "ix_projects_ownerid");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Name).HasMaxLength(100);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Owner).WithMany(p => p.Projects).HasForeignKey(d => d.OwnerId);
        });

        modelBuilder.Entity<ProjectMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ProjectMembers_pkey");

            entity.HasIndex(e => new { e.ProjectId, e.UserId }, "UQ_ProjectMembers_Project_User").IsUnique();

            entity.HasIndex(e => e.ProjectId, "ix_projectmembers_projectid");

            entity.HasIndex(e => e.RoleId, "ix_projectmembers_roleid");

            entity.HasIndex(e => e.UserId, "ix_projectmembers_userid");

            entity.Property(e => e.JoinedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectMembers).HasForeignKey(d => d.ProjectId);

            entity.HasOne(d => d.Role).WithMany(p => p.ProjectMembers)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.User).WithMany(p => p.ProjectMembers).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<ProjectMemberRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ProjectMemberRoles_pkey");

            entity.HasIndex(e => e.Name, "ProjectMemberRoles_Name_key").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<ProjectTask>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ProjectTasks_pkey");

            entity.HasIndex(e => e.AssigneeId, "ix_projecttasks_assigneeid");

            entity.HasIndex(e => e.DueDate, "ix_projecttasks_duedate");

            entity.HasIndex(e => e.PriorityId, "ix_projecttasks_priorityid");

            entity.HasIndex(e => e.ProjectId, "ix_projecttasks_projectid");

            entity.HasIndex(e => e.StatusId, "ix_projecttasks_statusid");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.PriorityId).HasDefaultValue(2);
            entity.Property(e => e.StatusId).HasDefaultValue(1);
            entity.Property(e => e.Title).HasMaxLength(200);

            entity.HasOne(d => d.Assignee).WithMany(p => p.ProjectTasks)
                .HasForeignKey(d => d.AssigneeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(d => d.Priority).WithMany(p => p.ProjectTasks)
                .HasForeignKey(d => d.PriorityId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasOne(d => d.Project).WithMany(p => p.ProjectTasks).HasForeignKey(d => d.ProjectId);

            entity.HasOne(d => d.Status).WithMany(p => p.ProjectTasks)
                .HasForeignKey(d => d.StatusId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<ProjectTaskPriority>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ProjectTaskPriorities_pkey");

            entity.HasIndex(e => e.Name, "ProjectTaskPriorities_Name_key").IsUnique();

            entity.Property(e => e.Color).HasMaxLength(20);
            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<ProjectTaskStatus>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("ProjectTaskStatuses_pkey");

            entity.HasIndex(e => e.Name, "ProjectTaskStatuses_Name_key").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("RefreshTokens_pkey");

            entity.HasIndex(e => e.Token, "RefreshTokens_Token_key").IsUnique();

            entity.HasIndex(e => e.ExpiresAt, "ix_refreshtokens_expiresat");

            entity.HasIndex(e => e.Token, "ix_refreshtokens_token");

            entity.HasIndex(e => e.UserId, "ix_refreshtokens_userid");

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Token).HasMaxLength(255);

            entity.HasOne(d => d.User).WithMany(p => p.RefreshTokens).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Roles_pkey");

            entity.HasIndex(e => e.Name, "Roles_Name_key").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Description).HasMaxLength(255);
            entity.Property(e => e.Name).HasMaxLength(50);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Users_pkey");

            entity.HasIndex(e => e.Email, "Users_Email_key").IsUnique();

            entity.HasIndex(e => e.Username, "Users_Username_key").IsUnique();

            entity.Property(e => e.CreatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Email).HasMaxLength(100);
            entity.Property(e => e.PasswordHash).HasMaxLength(255);
            entity.Property(e => e.UpdatedAt).HasDefaultValueSql("CURRENT_TIMESTAMP");
            entity.Property(e => e.Username).HasMaxLength(50);

            entity.HasOne(d => d.Role).WithMany(p => p.Users)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
