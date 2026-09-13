using System.ComponentModel.DataAnnotations;

namespace SchoolSchedule.Models.ViewModels
{
    // ViewModel لعرض جدول الصف
    public class ClassScheduleViewModel
    {
        public int ClassId { get; set; }
        public string ClassName { get; set; }
        public int Grade { get; set; }
        public int Section { get; set; }
        public string AcademicYear { get; set; }
        public int Semester { get; set; }

        // الجدول منظم حسب اليوم والحصة
        public Dictionary<int, Dictionary<int, ScheduleCell>> ScheduleGrid { get; set; } 
            = new Dictionary<int, Dictionary<int, ScheduleCell>>();

        public List<SchoolDay> Days { get; set; } = new List<SchoolDay>();
        public List<Period> Periods { get; set; } = new List<Period>();
        
        // للطباعة - تحويل Grid إلى قوائم
        public List<List<ScheduleCell?>> ScheduleGridList
        {
            get
            {
                var result = new List<List<ScheduleCell?>>();
                foreach (var day in Days.OrderBy(d => d.DayOrder))
                {
                    var daySchedules = new List<ScheduleCell?>();
                    foreach (var period in Periods.OrderBy(p => p.PeriodNumber))
                    {
                        if (ScheduleGrid.ContainsKey(day.DayId) && 
                            ScheduleGrid[day.DayId].ContainsKey(period.PeriodId))
                        {
                            daySchedules.Add(ScheduleGrid[day.DayId][period.PeriodId]);
                        }
                        else
                        {
                            daySchedules.Add(null);
                        }
                    }
                    result.Add(daySchedules);
                }
                return result;
            }
        }

        public List<string> DayNames => Days.OrderBy(d => d.DayOrder).Select(d => d.DayName).ToList();
        public List<int> PeriodNumbers => Periods.OrderBy(p => p.PeriodNumber).Select(p => p.PeriodNumber).ToList();
        public List<string> PeriodTimes => Periods.OrderBy(p => p.PeriodNumber).Select(p => p.TimeRange).ToList();
    }

    // ViewModel لعرض جدول المعلم
    public class TeacherScheduleViewModel
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; }
        public string SubjectName { get; set; }
        public string AcademicYear { get; set; }
        public int Semester { get; set; }
        public int TotalPeriods { get; set; }
        public bool IsHeadOfDepartment { get; set; }
        public bool IsSupervisor { get; set; }

        // الجدول منظم حسب اليوم والحصة
        public Dictionary<int, Dictionary<int, ScheduleCell>> ScheduleGrid { get; set; } 
            = new Dictionary<int, Dictionary<int, ScheduleCell>>();

        public List<SchoolDay> Days { get; set; } = new List<SchoolDay>();
        public List<Period> Periods { get; set; } = new List<Period>();
        
        // للطباعة - تحويل Grid إلى قوائم
        public List<List<ScheduleCell?>> ScheduleGridList
        {
            get
            {
                var result = new List<List<ScheduleCell?>>();
                foreach (var day in Days.OrderBy(d => d.DayOrder))
                {
                    var daySchedules = new List<ScheduleCell?>();
                    foreach (var period in Periods.OrderBy(p => p.PeriodNumber))
                    {
                        if (ScheduleGrid.ContainsKey(day.DayId) && 
                            ScheduleGrid[day.DayId].ContainsKey(period.PeriodId))
                        {
                            daySchedules.Add(ScheduleGrid[day.DayId][period.PeriodId]);
                        }
                        else
                        {
                            daySchedules.Add(null);
                        }
                    }
                    result.Add(daySchedules);
                }
                return result;
            }
        }

        public List<string> DayNames => Days.OrderBy(d => d.DayOrder).Select(d => d.DayName).ToList();
        public List<int> PeriodNumbers => Periods.OrderBy(p => p.PeriodNumber).Select(p => p.PeriodNumber).ToList();
        public List<string> PeriodTimes => Periods.OrderBy(p => p.PeriodNumber).Select(p => p.TimeRange).ToList();
        public int NumberOfClasses { get; set; }
        public List<string> Classes { get; set; } = new List<string>();
    }

    // خلية في الجدول
    public class ScheduleCell
    {
        public int ScheduleId { get; set; }
        public string SubjectName { get; set; }
        public string TeacherName { get; set; }
        public string ClassName { get; set; }
        public string Notes { get; set; }
        public bool IsEmpty => ScheduleId == 0;
    }

    // ViewModel لإنشاء/تعديل حصة
    public class ScheduleFormViewModel
    {
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

        // للعرض فقط
        public string? ClassName { get; set; }
        public string? SubjectName { get; set; }
        public string? TeacherName { get; set; }
        public string? DayName { get; set; }
        public int? PeriodNumber { get; set; }
    }

    // ViewModel لإحصائيات المعلم
    public class TeacherWorkloadViewModel
    {
        public int TeacherId { get; set; }
        public string TeacherName { get; set; }
        public string SubjectName { get; set; }
        public int TotalPeriods { get; set; }
        public int NumberOfClasses { get; set; }
        public bool IsHeadOfDepartment { get; set; }
        public bool IsSupervisor { get; set; }
        public string Notes { get; set; }
        public List<ClassScheduleSummary> ClassSchedules { get; set; } = new List<ClassScheduleSummary>();
    }

    public class ClassScheduleSummary
    {
        public string ClassName { get; set; }
        public int PeriodsCount { get; set; }
        public List<DaySchedule> DaySchedules { get; set; } = new List<DaySchedule>();
    }

    public class DaySchedule
    {
        public string DayName { get; set; }
        public List<int> PeriodNumbers { get; set; } = new List<int>();
    }

    // ViewModel للبحث
    public class ScheduleSearchViewModel
    {
        [Display(Name = "الصف")]
        public int? ClassId { get; set; }

        [Display(Name = "المادة")]
        public int? SubjectId { get; set; }

        [Display(Name = "المعلم")]
        public int? TeacherId { get; set; }

        [Display(Name = "اليوم")]
        public int? DayId { get; set; }

        [Display(Name = "الحصة")]
        public int? PeriodId { get; set; }

        [Display(Name = "السنة الدراسية")]
        public string? AcademicYear { get; set; }

        [Display(Name = "الفصل الدراسي")]
        public int? Semester { get; set; }

        public List<Schedule> Results { get; set; } = new List<Schedule>();
    }

    // ViewModel لاستيراد البيانات من Excel
    public class ImportScheduleViewModel
    {
        [Required(ErrorMessage = "الملف مطلوب")]
        [Display(Name = "ملف Excel")]
        public IFormFile ExcelFile { get; set; }

        [Required(ErrorMessage = "السنة الدراسية مطلوبة")]
        [Display(Name = "السنة الدراسية")]
        public string AcademicYear { get; set; }

        [Required(ErrorMessage = "الفصل الدراسي مطلوب")]
        [Display(Name = "الفصل الدراسي")]
        public int Semester { get; set; }

        [Display(Name = "حذف البيانات الموجودة")]
        public bool DeleteExisting { get; set; } = true;
    }

    // نتيجة الاستيراد
    public class ImportResult
    {
        public bool Success { get; set; }
        public int TeachersAdded { get; set; }
        public int SchedulesAdded { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
    }

    // ViewModel للنسخ السريع
    public class QuickCopyViewModel
    {
        [Required(ErrorMessage = "الصف المصدر مطلوب")]
        [Display(Name = "من الصف")]
        public int FromClassId { get; set; }

        [Required(ErrorMessage = "الصف الهدف مطلوب")]
        [Display(Name = "إلى الصف")]
        public int ToClassId { get; set; }

        [Display(Name = "السنة الدراسية")]
        public string? AcademicYear { get; set; }

        [Display(Name = "الفصل الدراسي")]
        public int? Semester { get; set; }

        [Display(Name = "حذف جدول الصف الهدف")]
        public bool DeleteExisting { get; set; } = true;
    }

    // ViewModel للتقارير
    public class ScheduleReportViewModel
    {
        public string ReportTitle { get; set; }
        public string AcademicYear { get; set; }
        public int Semester { get; set; }
        public DateTime GeneratedDate { get; set; } = DateTime.Now;

        public int TotalClasses { get; set; }
        public int TotalTeachers { get; set; }
        public int TotalScheduleEntries { get; set; }

        public List<SubjectStatistics> SubjectStats { get; set; } = new List<SubjectStatistics>();
        public List<TeacherWorkloadViewModel> TeacherWorkloads { get; set; } = new List<TeacherWorkloadViewModel>();
    }

    public class SubjectStatistics
    {
        public string SubjectName { get; set; }
        public int TeachersCount { get; set; }
        public int TotalPeriods { get; set; }
        public double AveragePeriodsPerTeacher { get; set; }
    }

    // ViewModel لعرض جدول يوم واحد
    public class DailyScheduleViewModel
    {
        public int DayId { get; set; }
        public string DayName { get; set; }
        public DateTime Date { get; set; }
        public string AcademicYear { get; set; }
        public int Semester { get; set; }

        // الصفوف والحصص
        public List<Class> Classes { get; set; } = new List<Class>();
        public List<Period> Periods { get; set; } = new List<Period>();

        // الجدول: ClassId -> PeriodId -> ScheduleCell
        public Dictionary<int, Dictionary<int, ScheduleCell>> ScheduleGrid { get; set; } 
            = new Dictionary<int, Dictionary<int, ScheduleCell>>();
    }
}
