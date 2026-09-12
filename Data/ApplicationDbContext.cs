using Microsoft.EntityFrameworkCore;
using SchoolSchedule.Models;

namespace SchoolSchedule.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Subject> Subjects { get; set; }
        public DbSet<Teacher> Teachers { get; set; }
        public DbSet<Class> Classes { get; set; }
        public DbSet<SchoolDay> SchoolDays { get; set; }
        public DbSet<Period> Periods { get; set; }
        public DbSet<Schedule> Schedules { get; set; }
        public DbSet<SchoolSetting> SchoolSettings { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // تحديد أسماء الجداول بشكل صريح
            modelBuilder.Entity<Subject>().ToTable("Subjects");
            modelBuilder.Entity<Teacher>().ToTable("Teachers");
            modelBuilder.Entity<Class>().ToTable("Classes");
            modelBuilder.Entity<SchoolDay>().ToTable("SchoolDays");
            modelBuilder.Entity<Period>().ToTable("Periods");
            modelBuilder.Entity<Schedule>().ToTable("Schedule");
            modelBuilder.Entity<SchoolSetting>().ToTable("SchoolSettings");

            // تكوين Class - Unique constraint على Grade و Section
            modelBuilder.Entity<Class>()
                .HasIndex(c => new { c.Grade, c.Section })
                .IsUnique();

            // تكوين ClassName و ClassCode كـ Computed Columns
            modelBuilder.Entity<Class>()
                .Property(c => c.ClassName)
                .HasComputedColumnSql("(CAST([Grade] AS VARCHAR) + '/' + CAST([Section] AS VARCHAR))", stored: true);

            modelBuilder.Entity<Class>()
                .Property(c => c.ClassCode)
                .HasComputedColumnSql("(CAST([Grade] AS VARCHAR) + CAST([Section] AS VARCHAR))", stored: true);

            // تكوين SchoolDay - Unique constraint على DayOrder
            modelBuilder.Entity<SchoolDay>()
                .HasIndex(d => d.DayOrder)
                .IsUnique();

            // تكوين Period - Unique constraint على PeriodNumber
            modelBuilder.Entity<Period>()
                .HasIndex(p => p.PeriodNumber)
                .IsUnique();

            // تكوين Schedule - Unique constraint
            modelBuilder.Entity<Schedule>()
                .HasIndex(s => new { s.ClassId, s.DayId, s.PeriodId, s.AcademicYear, s.Semester })
                .IsUnique();

            // تكوين العلاقات
            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Class)
                .WithMany(c => c.Schedules)
                .HasForeignKey(s => s.ClassId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Subject)
                .WithMany(sub => sub.Schedules)
                .HasForeignKey(s => s.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Teacher)
                .WithMany(t => t.Schedules)
                .HasForeignKey(s => s.TeacherId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.SchoolDay)
                .WithMany(d => d.Schedules)
                .HasForeignKey(s => s.DayId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Schedule>()
                .HasOne(s => s.Period)
                .WithMany(p => p.Schedules)
                .HasForeignKey(s => s.PeriodId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Teacher>()
                .HasOne(t => t.Subject)
                .WithMany(s => s.Teachers)
                .HasForeignKey(t => t.SubjectId)
                .OnDelete(DeleteBehavior.Restrict);

            // Seed Data - البيانات الأساسية

            // School Settings
            modelBuilder.Entity<SchoolSetting>().HasData(
                new SchoolSetting
                {
                    SettingId = 1,
                    SchoolName = "مدرسة عبدالمحسن الخرافى المتوسطة للبنين",
                    EducationalZone = "الادارة العامة لمنطقة حولى التعليمية",
                    CurrentAcademicYear = "2025/2026",
                    CurrentSemester = 2,
                    SemesterStartDate = new DateTime(2026, 1, 25),
                    PeriodsPerDay = 7,
                    CreatedDate = DateTime.Now
                }
            );

            // School Days
            modelBuilder.Entity<SchoolDay>().HasData(
                new SchoolDay { DayId = 1, DayName = "الأحد", DayNameEn = "Sunday", DayOrder = 1 },
                new SchoolDay { DayId = 2, DayName = "الاثنين", DayNameEn = "Monday", DayOrder = 2 },
                new SchoolDay { DayId = 3, DayName = "الثلاثاء", DayNameEn = "Tuesday", DayOrder = 3 },
                new SchoolDay { DayId = 4, DayName = "الأربعاء", DayNameEn = "Wednesday", DayOrder = 4 },
                new SchoolDay { DayId = 5, DayName = "الخميس", DayNameEn = "Thursday", DayOrder = 5 }
            );

            // Periods
            modelBuilder.Entity<Period>().HasData(
                new Period { PeriodId = 1, PeriodNumber = 1, StartTime = new TimeSpan(7, 30, 0), EndTime = new TimeSpan(8, 15, 0) },
                new Period { PeriodId = 2, PeriodNumber = 2, StartTime = new TimeSpan(8, 15, 0), EndTime = new TimeSpan(9, 0, 0) },
                new Period { PeriodId = 3, PeriodNumber = 3, StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(9, 45, 0) },
                new Period { PeriodId = 4, PeriodNumber = 4, StartTime = new TimeSpan(10, 0, 0), EndTime = new TimeSpan(10, 45, 0) },
                new Period { PeriodId = 5, PeriodNumber = 5, StartTime = new TimeSpan(10, 45, 0), EndTime = new TimeSpan(11, 30, 0) },
                new Period { PeriodId = 6, PeriodNumber = 6, StartTime = new TimeSpan(11, 30, 0), EndTime = new TimeSpan(12, 15, 0) },
                new Period { PeriodId = 7, PeriodNumber = 7, StartTime = new TimeSpan(12, 15, 0), EndTime = new TimeSpan(13, 0, 0) }
            );

            // Subjects
            modelBuilder.Entity<Subject>().HasData(
                new Subject { SubjectId = 1, SubjectName = "التربية الإسلامية", SubjectNameEn = "Islamic Education" },
                new Subject { SubjectId = 2, SubjectName = "اللغة العربية", SubjectNameEn = "Arabic Language" },
                new Subject { SubjectId = 3, SubjectName = "اللغة الإنجليزية", SubjectNameEn = "English Language" },
                new Subject { SubjectId = 4, SubjectName = "الرياضيات", SubjectNameEn = "Mathematics" },
                new Subject { SubjectId = 5, SubjectName = "العلوم", SubjectNameEn = "Science" },
                new Subject { SubjectId = 6, SubjectName = "الاجتماعيات", SubjectNameEn = "Social Studies" },
                new Subject { SubjectId = 7, SubjectName = "الحاسوب", SubjectNameEn = "Computer" },
                new Subject { SubjectId = 8, SubjectName = "التربية البدنية", SubjectNameEn = "Physical Education" },
                new Subject { SubjectId = 9, SubjectName = "التربية الموسيقية", SubjectNameEn = "Music" },
                new Subject { SubjectId = 10, SubjectName = "التربية الفنية", SubjectNameEn = "Art" },
                new Subject { SubjectId = 11, SubjectName = "الدراسات العملية", SubjectNameEn = "Practical Studies" }
            );

            // Classes - من الصف 6 إلى 9، كل صف له 9 شعب
            var classId = 1;
            var classes = new List<Class>();
            for (int grade = 6; grade <= 9; grade++)
            {
                for (int section = 1; section <= 9; section++)
                {
                    classes.Add(new Class
                    {
                        ClassId = classId++,
                        Grade = grade,
                        Section = section,
                        IsActive = true,
                        CreatedDate = DateTime.Now
                    });
                }
            }
            modelBuilder.Entity<Class>().HasData(classes);
        }
    }
}
