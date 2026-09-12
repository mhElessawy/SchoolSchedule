using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SchoolSchedule.Models
{
    // المواد الدراسية
    public class Subject
    {
        [Key]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "اسم المادة مطلوب")]
        [StringLength(100)]
        [Display(Name = "اسم المادة")]
        public string SubjectName { get; set; }

        [StringLength(100)]
        [Display(Name = "Subject Name (English)")]
        public string? SubjectNameEn { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual ICollection<Teacher> Teachers { get; set; } = new List<Teacher>();
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    }

    // المعلمين
    public class Teacher
    {
        [Key]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "اسم المعلم مطلوب")]
        [StringLength(100)]
        [Display(Name = "اسم المعلم")]
        public string TeacherName { get; set; }

        [Required]
        [Display(Name = "المادة")]
        public int SubjectId { get; set; }

        [Display(Name = "رئيس قسم")]
        public bool IsHeadOfDepartment { get; set; } = false;

        [Display(Name = "مشرف جناح")]
        public bool IsSupervisor { get; set; } = false;

        [StringLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        [ForeignKey("SubjectId")]
        public virtual Subject Subject { get; set; }
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    }

    // الصفوف الدراسية
    public class Class
    {
        [Key]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "الصف مطلوب")]
        [Range(6, 9, ErrorMessage = "الصف يجب أن يكون بين 6 و 9")]
        [Display(Name = "الصف")]
        public int Grade { get; set; }

        [Required(ErrorMessage = "الشعبة مطلوبة")]
        [Range(1, 9, ErrorMessage = "الشعبة يجب أن تكون بين 1 و 9")]
        [Display(Name = "الشعبة")]
        public int Section { get; set; }

        [Display(Name = "اسم الصف")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? ClassName { get; set; }

        [Display(Name = "كود الصف")]
        [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public string? ClassCode { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        // Navigation Properties
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

        [NotMapped]
        [Display(Name = "اسم الصف الكامل")]
        public string DisplayName => $"{Grade}/{Section}";
    }

    // الأيام الدراسية
    public class SchoolDay
    {
        [Key]
        public int DayId { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "اسم اليوم")]
        public string DayName { get; set; }

        [Required]
        [StringLength(50)]
        [Display(Name = "Day Name (English)")]
        public string DayNameEn { get; set; }

        [Required]
        [Display(Name = "ترتيب اليوم")]
        public int DayOrder { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();
    }

    // الحصص (الفترات)
    public class Period
    {
        [Key]
        public int PeriodId { get; set; }

        [Required]
        [Display(Name = "رقم الحصة")]
        public int PeriodNumber { get; set; }

        [Display(Name = "وقت البداية")]
        public TimeSpan? StartTime { get; set; }

        [Display(Name = "وقت النهاية")]
        public TimeSpan? EndTime { get; set; }

        [Display(Name = "نشط")]
        public bool IsActive { get; set; } = true;

        // Navigation Properties
        public virtual ICollection<Schedule> Schedules { get; set; } = new List<Schedule>();

        [NotMapped]
        public string TimeRange => StartTime.HasValue && EndTime.HasValue 
            ? $"{StartTime:hh\\:mm} - {EndTime:hh\\:mm}" 
            : "";
    }

    // الجدول المدرسي
    public class Schedule
    {
        [Key]
        public int ScheduleId { get; set; }

        [Required(ErrorMessage = "الصف مطلوب")]
        [Display(Name = "الصف")]
        public int ClassId { get; set; }

        [Required(ErrorMessage = "المادة مطلوبة")]
        [Display(Name = "المادة")]
        public int SubjectId { get; set; }

        [Required(ErrorMessage = "المعلم مطلوب")]
        [Display(Name = "المعلم")]
        public int TeacherId { get; set; }

        [Required(ErrorMessage = "اليوم مطلوب")]
        [Display(Name = "اليوم")]
        public int DayId { get; set; }

        [Required(ErrorMessage = "الحصة مطلوبة")]
        [Display(Name = "الحصة")]
        public int PeriodId { get; set; }

        [StringLength(20)]
        [Display(Name = "السنة الدراسية")]
        public string? AcademicYear { get; set; }

        [Display(Name = "الفصل الدراسي")]
        public int? Semester { get; set; }

        [StringLength(500)]
        [Display(Name = "ملاحظات")]
        public string? Notes { get; set; }

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "تاريخ التحديث")]
        public DateTime? UpdatedDate { get; set; }

        // Navigation Properties
        [ForeignKey("ClassId")]
        public virtual Class Class { get; set; }

        [ForeignKey("SubjectId")]
        public virtual Subject Subject { get; set; }

        [ForeignKey("TeacherId")]
        public virtual Teacher Teacher { get; set; }

        [ForeignKey("DayId")]
        public virtual SchoolDay SchoolDay { get; set; }

        [ForeignKey("PeriodId")]
        public virtual Period Period { get; set; }
    }

    // إعدادات المدرسة
    public class SchoolSetting
    {
        [Key]
        public int SettingId { get; set; }

        [Required]
        [StringLength(200)]
        [Display(Name = "اسم المدرسة")]
        public string SchoolName { get; set; }

        [StringLength(200)]
        [Display(Name = "المنطقة التعليمية")]
        public string? EducationalZone { get; set; }

        [StringLength(20)]
        [Display(Name = "السنة الدراسية الحالية")]
        public string? CurrentAcademicYear { get; set; }

        [Display(Name = "الفصل الدراسي الحالي")]
        public int? CurrentSemester { get; set; }

        [Display(Name = "تاريخ بداية الفصل")]
        public DateTime? SemesterStartDate { get; set; }

        [Display(Name = "عدد الحصص اليومية")]
        public int PeriodsPerDay { get; set; } = 7;

        [Display(Name = "تاريخ الإنشاء")]
        public DateTime CreatedDate { get; set; } = DateTime.Now;

        [Display(Name = "تاريخ التحديث")]
        public DateTime? UpdatedDate { get; set; }
    }
}
