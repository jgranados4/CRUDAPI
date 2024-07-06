using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace CRUDAPI.Models;

public partial class HolamundoContext : DbContext
{
    public HolamundoContext()
    {
    }

    public HolamundoContext(DbContextOptions<HolamundoContext> options)
        : base(options)
    {
    }


    public virtual DbSet<Usuario> Usuarios { get; set; }
    public virtual DbSet<UsuarioAU> UsuariosAU { get; set; }    

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .UseCollation("utf8mb4_0900_ai_ci")
            .HasCharSet("utf8mb4");

        
        modelBuilder.Entity<Usuario>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("usuario");

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Estado)
                .HasMaxLength(255)
                .HasColumnName("estado");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("nombre");
            entity.Property(e => e.Apellido)
                .HasMaxLength(255)
                .HasColumnName("Apellido");
        });
        modelBuilder.Entity<UsuarioAU>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");
            entity.ToTable("usuariosau");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Nombre)
                .HasMaxLength(255)
                .HasColumnName("Nombre");
            entity.Property(e => e.Constrasena)
                .HasMaxLength(300)
                .HasColumnName("constrasena");
            entity.Property(e => e.Email)
                .HasMaxLength(300)
                .HasColumnName("email");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
