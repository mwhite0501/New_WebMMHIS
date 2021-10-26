using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

#nullable disable

namespace NewMMHIS_Web.Models
{
    public partial class mmhisContext : DbContext
    {
        public mmhisContext()
        {
        }

        public mmhisContext(DbContextOptions<mmhisContext> options)
            : base(options)
        {
        }

        public virtual DbSet<MmhisDamu> MmhisDamus { get; set; }
        public virtual DbSet<MmhisDamuBadRun> MmhisDamuBadRuns { get; set; }
        public virtual DbSet<MmhisDian> MmhisDians { get; set; }
        public virtual DbSet<MmhisDuan> MmhisDuans { get; set; }
        public virtual DbSet<MmhisFen> MmhisFens { get; set; }
        public virtual DbSet<MmhisMapDamu> MmhisMapDamus { get; set; }
        public virtual DbSet<MmhisMapFen> MmhisMapFens { get; set; }
        public virtual DbSet<MmhisMapObject> MmhisMapObjects { get; set; }
        public virtual DbSet<MmhisMapPoint> MmhisMapPoints { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. You can avoid scaffolding the connection string by using the Name= syntax to read it from configuration - see https://go.microsoft.com/fwlink/?linkid=2131148. For more guidance on storing connection strings, see http://go.microsoft.com/fwlink/?LinkId=723263.
                optionsBuilder.UseSqlServer("Server=INVSQL-DEV;Database=mmhis;Trusted_Connection=True;");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("Relational:Collation", "SQL_Latin1_General_CP1_CI_AS");

            modelBuilder.Entity<MmhisDamu>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("mmhis_damu");

                entity.HasIndex(e => new { e.County, e.County1, e.Route, e.Section, e.MmhisDirection, e.TheYear }, "index_damu_0");

                entity.HasIndex(e => e.Ld, "index_damu_1")
                    .IsUnique();

                entity.HasIndex(e => new { e.District, e.District1, e.County, e.County1, e.Route, e.Section, e.MmhisDirection, e.TheYear }, "index_damu_2");

                entity.Property(e => e.ArnoldDirection)
                    .IsRequired()
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasColumnName("arnold_direction");

                entity.Property(e => e.ComputerHostName)
                    .HasMaxLength(100)
                    .HasColumnName("computer_host_name");

                entity.Property(e => e.County)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("county");

                entity.Property(e => e.County1)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("county_1");

                entity.Property(e => e.District)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("district");

                entity.Property(e => e.District1)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("district_1");

                entity.Property(e => e.FrameLoadingOrder).HasColumnName("frame_loading_order");

                entity.Property(e => e.Ld).HasColumnName("ld");

                entity.Property(e => e.MmhisDirection)
                    .HasMaxLength(5)
                    .IsUnicode(false)
                    .HasColumnName("mmhis_direction");

                entity.Property(e => e.Note)
                    .HasMaxLength(2000)
                    .HasColumnName("note");

                entity.Property(e => e.Route)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("route");

                entity.Property(e => e.Section)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("section");

                entity.Property(e => e.TheSystem)
                    .HasMaxLength(30)
                    .HasColumnName("the_system");

                entity.Property(e => e.TheYear)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("the_year");

                entity.Property(e => e.TimeStamp)
                    .HasMaxLength(18)
                    .IsUnicode(false)
                    .HasColumnName("time_stamp");

                entity.Property(e => e.UserId)
                    .HasMaxLength(100)
                    .HasColumnName("user_id");
            });

            modelBuilder.Entity<MmhisDamuBadRun>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("mmhis_damu_bad_runs");

                entity.HasIndex(e => new { e.County, e.County1, e.Route, e.Section, e.MmhisDirection, e.TheYear }, "index_damu_bad_runs_0");

                entity.HasIndex(e => e.Ld, "index_damu_bad_runs_1")
                    .IsUnique();

                entity.HasIndex(e => new { e.District, e.District1, e.County, e.County1, e.Route, e.Section, e.MmhisDirection, e.TheYear }, "index_damu_bad_runs_2");

                entity.Property(e => e.ArnoldDirection)
                    .IsRequired()
                    .HasMaxLength(3)
                    .IsUnicode(false)
                    .HasColumnName("arnold_direction");

                entity.Property(e => e.ComputerHostName)
                    .HasMaxLength(100)
                    .HasColumnName("computer_host_name");

                entity.Property(e => e.County)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("county");

                entity.Property(e => e.County1)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("county_1");

                entity.Property(e => e.District)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("district");

                entity.Property(e => e.District1)
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("district_1");

                entity.Property(e => e.FrameLoadingOrder).HasColumnName("frame_loading_order");

                entity.Property(e => e.Ld).HasColumnName("ld");

                entity.Property(e => e.MmhisDirection)
                    .HasMaxLength(5)
                    .IsUnicode(false)
                    .HasColumnName("mmhis_direction");

                entity.Property(e => e.Note)
                    .HasMaxLength(2000)
                    .HasColumnName("note");

                entity.Property(e => e.Route)
                    .IsRequired()
                    .HasMaxLength(100)
                    .IsUnicode(false)
                    .HasColumnName("route");

                entity.Property(e => e.Section)
                    .IsRequired()
                    .HasMaxLength(10)
                    .IsUnicode(false)
                    .HasColumnName("section");

                entity.Property(e => e.TheSystem)
                    .HasMaxLength(30)
                    .HasColumnName("the_system");

                entity.Property(e => e.TheYear)
                    .IsRequired()
                    .HasMaxLength(20)
                    .IsUnicode(false)
                    .HasColumnName("the_year");

                entity.Property(e => e.TimeStamp)
                    .HasMaxLength(18)
                    .IsUnicode(false)
                    .HasColumnName("time_stamp");

                entity.Property(e => e.UserId)
                    .HasMaxLength(100)
                    .HasColumnName("user_id");
            });

            modelBuilder.Entity<MmhisDian>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("mmhis_dian");

                entity.HasIndex(e => new { e.Lu, e.Logmeter0, e.Latitude, e.Longitude }, "index_dian_0");

                entity.HasIndex(e => new { e.Latitude, e.Longitude }, "index_dian_1");

                entity.HasIndex(e => new { e.Lu, e.Latitude, e.Longitude }, "index_dian_2");

                entity.HasIndex(e => new { e.Lu, e.Ld }, "index_dian_3")
                    .IsUnique();

                entity.HasIndex(e => e.Ld, "index_dian_4");

                entity.Property(e => e.Latitude).HasColumnName("latitude");

                entity.Property(e => e.Ld).HasColumnName("ld");

                entity.Property(e => e.Logmeter0).HasColumnName("logmeter_0");

                entity.Property(e => e.Longitude).HasColumnName("longitude");

                entity.Property(e => e.Lu).HasColumnName("lu");
            });

            modelBuilder.Entity<MmhisDuan>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("mmhis_duan");

                entity.HasIndex(e => new { e.Lu, e.Logmeter0, e.Logmeter1 }, "index_duan_0");

                entity.HasIndex(e => new { e.Latitude, e.Longitude }, "index_duan_1");

                entity.HasIndex(e => new { e.Lu, e.Latitude, e.Longitude }, "index_duan_2");

                entity.HasIndex(e => new { e.Lu, e.Ld }, "index_duan_3");

                entity.HasIndex(e => e.Ld, "index_duan_4");

                entity.Property(e => e.Latitude).HasColumnName("latitude");

                entity.Property(e => e.Ld).HasColumnName("ld");

                entity.Property(e => e.Logmeter0).HasColumnName("logmeter_0");

                entity.Property(e => e.Logmeter1).HasColumnName("logmeter_1");

                entity.Property(e => e.Longitude).HasColumnName("longitude");

                entity.Property(e => e.Lu).HasColumnName("lu");
            });

            modelBuilder.Entity<MmhisFen>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("mmhis_fen");

                entity.HasIndex(e => new { e.Lu, e.FieldCategory, e.FieldName }, "index_fen_0");

                entity.HasIndex(e => e.FieldValue, "index_fen_1");

                entity.HasIndex(e => new { e.Lt, e.FieldCategory, e.FieldName }, "index_fen_2");

                entity.Property(e => e.FieldCategory)
                    .IsRequired()
                    .HasMaxLength(2)
                    .IsUnicode(false)
                    .HasColumnName("field_category");

                entity.Property(e => e.FieldName)
                    .IsRequired()
                    .HasMaxLength(40)
                    .IsUnicode(false)
                    .HasColumnName("field_name");

                entity.Property(e => e.FieldValue)
                    .IsRequired()
                    .HasMaxLength(300)
                    .IsUnicode(false)
                    .HasColumnName("field_value");

                entity.Property(e => e.Lt).HasColumnName("lt");

                entity.Property(e => e.Lu).HasColumnName("lu");
            });

            modelBuilder.Entity<MmhisMapDamu>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("mmhis_map_damu");

                entity.HasIndex(e => new { e.TheYear, e.Category, e.Key }, "index_map_damu_0");

                entity.HasIndex(e => new { e.TheYear, e.Latitude, e.Longitude }, "index_map_damu_1");

                entity.HasIndex(e => new { e.TheYear, e.Ld }, "index_map_damu_2")
                    .IsUnique();

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(40)
                    .IsUnicode(false)
                    .HasColumnName("category");

                entity.Property(e => e.Key)
                    .IsRequired()
                    .HasMaxLength(40)
                    .IsUnicode(false)
                    .HasColumnName("key");

                entity.Property(e => e.Latitude).HasColumnName("latitude");

                entity.Property(e => e.Ld).HasColumnName("ld");

                entity.Property(e => e.Longitude).HasColumnName("longitude");

                entity.Property(e => e.ObjectType)
                    .IsRequired()
                    .HasMaxLength(40)
                    .IsUnicode(false)
                    .HasColumnName("object_type");

                entity.Property(e => e.TheYear)
                    .IsRequired()
                    .HasMaxLength(16)
                    .IsUnicode(false)
                    .HasColumnName("the_year");

                entity.Property(e => e.Xu).HasColumnName("xu");
            });

            modelBuilder.Entity<MmhisMapFen>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("mmhis_map_fen");

                entity.HasIndex(e => new { e.Lu, e.FieldName }, "index_map_fen_0");

                entity.Property(e => e.FieldName)
                    .IsRequired()
                    .HasMaxLength(40)
                    .IsUnicode(false)
                    .HasColumnName("field_name");

                entity.Property(e => e.FieldValue)
                    .HasMaxLength(400)
                    .IsUnicode(false)
                    .HasColumnName("field_value");

                entity.Property(e => e.Lu).HasColumnName("lu");
            });

            modelBuilder.Entity<MmhisMapObject>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("mmhis_map_object");

                entity.HasIndex(e => new { e.Lu, e.Ld }, "index_map_object_0");

                entity.HasIndex(e => new { e.Ld, e.Lu }, "index_map_object_1");

                entity.Property(e => e.Ld).HasColumnName("ld");

                entity.Property(e => e.Lu).HasColumnName("lu");
            });

            modelBuilder.Entity<MmhisMapPoint>(entity =>
            {
                entity.HasNoKey();

                entity.ToTable("mmhis_map_point");

                entity.HasIndex(e => new { e.Lu, e.PointOrder }, "index_map_point_0");

                entity.HasIndex(e => new { e.Latitude, e.Longitude }, "index_map_point_1");

                entity.Property(e => e.Latitude).HasColumnName("latitude");

                entity.Property(e => e.Longitude).HasColumnName("longitude");

                entity.Property(e => e.Lu).HasColumnName("lu");

                entity.Property(e => e.PointOrder).HasColumnName("point_order");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
