using Microsoft.EntityFrameworkCore;
using SchoolSchedule.Data;
using SchoolSchedule.Models;
using SchoolSchedule.Models.ViewModels;

namespace SchoolSchedule.Services
{
    public class ScheduleService
    {
        private readonly ApplicationDbContext _context;

        public ScheduleService(ApplicationDbContext context)
        {
            _context = context;
        }

        // الحصول على جدول صف معين
        public async Task<ClassScheduleViewModel> GetClassSchedule(
            int classId, 
            string? academicYear = null, 
            int? semester = null)
        {
            var settings = await GetCurrentSettings();
            academicYear ??= settings.CurrentAcademicYear;
            semester ??= settings.CurrentSemester;

            var classEntity = await _context.Classes.FindAsync(classId);
            if (classEntity == null)
                throw new ArgumentException("الصف غير موجود");

            var viewModel = new ClassScheduleViewModel
            {
                ClassId = classId,
                ClassName = classEntity.ClassName!,
                Grade = classEntity.Grade,
                Section = classEntity.Section,
                AcademicYear = academicYear!,
                Semester = semester!.Value,
                Days = await _context.SchoolDays.OrderBy(d => d.DayOrder).ToListAsync(),
                Periods = await _context.Periods.OrderBy(p => p.PeriodNumber).ToListAsync()
            };

            // تحميل الحصص
            var schedules = await _context.Schedules
                .Include(s => s.Subject)
                .Include(s => s.Teacher)
                .Include(s => s.SchoolDay)
                .Include(s => s.Period)
                .Where(s => s.ClassId == classId && 
                           s.AcademicYear == academicYear && 
                           s.Semester == semester)
                .ToListAsync();

            // بناء الجدول
            foreach (var day in viewModel.Days)
            {
                viewModel.ScheduleGrid[day.DayId] = new Dictionary<int, ScheduleCell>();
                
                foreach (var period in viewModel.Periods)
                {
                    var schedule = schedules.FirstOrDefault(s => 
                        s.DayId == day.DayId && s.PeriodId == period.PeriodId);

                    if (schedule != null)
                    {
                        viewModel.ScheduleGrid[day.DayId][period.PeriodId] = new ScheduleCell
                        {
                            ScheduleId = schedule.ScheduleId,
                            SubjectName = schedule.Subject.SubjectName,
                            TeacherName = schedule.Teacher.TeacherName,
                            Notes = schedule.Notes
                        };
                    }
                    else
                    {
                        viewModel.ScheduleGrid[day.DayId][period.PeriodId] = new ScheduleCell();
                    }
                }
            }

            return viewModel;
        }

        // الحصول على جدول معلم معين
        public async Task<TeacherScheduleViewModel> GetTeacherSchedule(
            int teacherId, 
            string? academicYear = null, 
            int? semester = null)
        {
            var settings = await GetCurrentSettings();
            academicYear ??= settings.CurrentAcademicYear;
            semester ??= settings.CurrentSemester;

            var teacher = await _context.Teachers
                .Include(t => t.Subject)
                .FirstOrDefaultAsync(t => t.TeacherId == teacherId);

            if (teacher == null)
                throw new ArgumentException("المعلم غير موجود");

            var viewModel = new TeacherScheduleViewModel
            {
                TeacherId = teacherId,
                TeacherName = teacher.TeacherName,
                SubjectName = teacher.Subject.SubjectName,
                IsHeadOfDepartment = teacher.IsHeadOfDepartment,
                IsSupervisor = teacher.IsSupervisor,
                AcademicYear = academicYear!,
                Semester = semester!.Value,
                Days = await _context.SchoolDays.OrderBy(d => d.DayOrder).ToListAsync(),
                Periods = await _context.Periods.OrderBy(p => p.PeriodNumber).ToListAsync()
            };

            // تحميل الحصص
            var schedules = await _context.Schedules
                .Include(s => s.Class)
                .Include(s => s.Subject)
                .Include(s => s.SchoolDay)
                .Include(s => s.Period)
                .Where(s => s.TeacherId == teacherId && 
                           s.AcademicYear == academicYear && 
                           s.Semester == semester)
                .ToListAsync();

            viewModel.TotalPeriods = schedules.Count;
            
            // حساب عدد الصفوف وقائمة الصفوف
            var distinctClasses = schedules
                .Select(s => s.Class.ClassName!)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
            
            viewModel.NumberOfClasses = distinctClasses.Count;
            viewModel.Classes = distinctClasses;

            // بناء الجدول
            foreach (var day in viewModel.Days)
            {
                viewModel.ScheduleGrid[day.DayId] = new Dictionary<int, ScheduleCell>();
                
                foreach (var period in viewModel.Periods)
                {
                    var schedule = schedules.FirstOrDefault(s => 
                        s.DayId == day.DayId && s.PeriodId == period.PeriodId);

                    if (schedule != null)
                    {
                        viewModel.ScheduleGrid[day.DayId][period.PeriodId] = new ScheduleCell
                        {
                            ScheduleId = schedule.ScheduleId,
                            ClassName = schedule.Class.ClassName!,
                            SubjectName = schedule.Subject.SubjectName,
                            Notes = schedule.Notes
                        };
                    }
                    else
                    {
                        viewModel.ScheduleGrid[day.DayId][period.PeriodId] = new ScheduleCell();
                    }
                }
            }

            return viewModel;
        }

        // التحقق من تعارض الجدول
        public async Task<(bool HasConflict, string Message)> CheckScheduleConflict(
            int classId, 
            int teacherId, 
            int dayId, 
            int periodId, 
            string academicYear, 
            int semester,
            int? excludeScheduleId = null)
        {
            // التحقق من تعارض الصف
            var classConflict = await _context.Schedules
                .AnyAsync(s => s.ClassId == classId &&
                              s.DayId == dayId &&
                              s.PeriodId == periodId &&
                              s.AcademicYear == academicYear &&
                              s.Semester == semester &&
                              (!excludeScheduleId.HasValue || s.ScheduleId != excludeScheduleId.Value));

            if (classConflict)
                return (true, "الصف لديه حصة في نفس الوقت");

            // التحقق من تعارض المعلم
            var teacherConflict = await _context.Schedules
                .AnyAsync(s => s.TeacherId == teacherId &&
                              s.DayId == dayId &&
                              s.PeriodId == periodId &&
                              s.AcademicYear == academicYear &&
                              s.Semester == semester &&
                              (!excludeScheduleId.HasValue || s.ScheduleId != excludeScheduleId.Value));

            if (teacherConflict)
                return (true, "المعلم لديه حصة في نفس الوقت");

            return (false, "لا يوجد تعارض");
        }

        // إضافة حصة جديدة
        public async Task<(bool Success, string Message)> AddSchedule(ScheduleFormViewModel model)
        {
            // التحقق من التعارض
            var (hasConflict, message) = await CheckScheduleConflict(
                model.ClassId, 
                model.TeacherId, 
                model.DayId, 
                model.PeriodId, 
                model.AcademicYear!, 
                model.Semester!.Value);

            if (hasConflict)
                return (false, message);

            var schedule = new Schedule
            {
                ClassId = model.ClassId,
                SubjectId = model.SubjectId,
                TeacherId = model.TeacherId,
                DayId = model.DayId,
                PeriodId = model.PeriodId,
                AcademicYear = model.AcademicYear,
                Semester = model.Semester,
                Notes = model.Notes
            };

            _context.Schedules.Add(schedule);
            await _context.SaveChangesAsync();

            return (true, "تم إضافة الحصة بنجاح");
        }

        // تحديث حصة
        public async Task<(bool Success, string Message)> UpdateSchedule(ScheduleFormViewModel model)
        {
            var schedule = await _context.Schedules.FindAsync(model.ScheduleId);
            if (schedule == null)
                return (false, "الحصة غير موجودة");

            // التحقق من التعارض
            var (hasConflict, message) = await CheckScheduleConflict(
                model.ClassId, 
                model.TeacherId, 
                model.DayId, 
                model.PeriodId, 
                model.AcademicYear!, 
                model.Semester!.Value,
                model.ScheduleId);

            if (hasConflict)
                return (false, message);

            schedule.ClassId = model.ClassId;
            schedule.SubjectId = model.SubjectId;
            schedule.TeacherId = model.TeacherId;
            schedule.DayId = model.DayId;
            schedule.PeriodId = model.PeriodId;
            schedule.Notes = model.Notes;
            schedule.UpdatedDate = DateTime.Now;

            await _context.SaveChangesAsync();

            return (true, "تم تحديث الحصة بنجاح");
        }

        // حذف حصة
        public async Task<bool> DeleteSchedule(int scheduleId)
        {
            var schedule = await _context.Schedules.FindAsync(scheduleId);
            if (schedule == null)
                return false;

            _context.Schedules.Remove(schedule);
            await _context.SaveChangesAsync();

            return true;
        }

        // نسخ جدول من صف لآخر
        public async Task<(bool Success, string Message, int CopiedCount)> CopySchedule(QuickCopyViewModel model)
        {
            var settings = await GetCurrentSettings();
            model.AcademicYear ??= settings.CurrentAcademicYear;
            model.Semester ??= settings.CurrentSemester;

            var sourceSchedules = await _context.Schedules
                .Where(s => s.ClassId == model.FromClassId &&
                           s.AcademicYear == model.AcademicYear &&
                           s.Semester == model.Semester)
                .ToListAsync();

            if (!sourceSchedules.Any())
                return (false, "الصف المصدر ليس لديه جدول", 0);

            // حذف الجدول الهدف إذا طُلب ذلك
            if (model.DeleteExisting)
            {
                var targetSchedules = await _context.Schedules
                    .Where(s => s.ClassId == model.ToClassId &&
                               s.AcademicYear == model.AcademicYear &&
                               s.Semester == model.Semester)
                    .ToListAsync();

                _context.Schedules.RemoveRange(targetSchedules);
            }

            int copiedCount = 0;
            foreach (var sourceSchedule in sourceSchedules)
            {
                // التحقق من عدم وجود تعارض
                var exists = await _context.Schedules
                    .AnyAsync(s => s.ClassId == model.ToClassId &&
                                  s.DayId == sourceSchedule.DayId &&
                                  s.PeriodId == sourceSchedule.PeriodId &&
                                  s.AcademicYear == model.AcademicYear &&
                                  s.Semester == model.Semester);

                if (!exists)
                {
                    var newSchedule = new Schedule
                    {
                        ClassId = model.ToClassId,
                        SubjectId = sourceSchedule.SubjectId,
                        TeacherId = sourceSchedule.TeacherId,
                        DayId = sourceSchedule.DayId,
                        PeriodId = sourceSchedule.PeriodId,
                        AcademicYear = model.AcademicYear,
                        Semester = model.Semester,
                        Notes = sourceSchedule.Notes
                    };

                    _context.Schedules.Add(newSchedule);
                    copiedCount++;
                }
            }

            await _context.SaveChangesAsync();

            return (true, $"تم نسخ {copiedCount} حصة بنجاح", copiedCount);
        }

        // إحصائيات المعلمين
        public async Task<List<TeacherWorkloadViewModel>> GetTeacherWorkloads(
            string? academicYear = null, 
            int? semester = null)
        {
            var settings = await GetCurrentSettings();
            academicYear ??= settings.CurrentAcademicYear;
            semester ??= settings.CurrentSemester;

            var teachers = await _context.Teachers
                .Include(t => t.Subject)
                .Where(t => t.IsActive)
                .ToListAsync();

            var result = new List<TeacherWorkloadViewModel>();

            foreach (var teacher in teachers)
            {
                var schedules = await _context.Schedules
                    .Include(s => s.Class)
                    .Include(s => s.SchoolDay)
                    .Include(s => s.Period)
                    .Where(s => s.TeacherId == teacher.TeacherId &&
                               s.AcademicYear == academicYear &&
                               s.Semester == semester)
                    .ToListAsync();

                var workload = new TeacherWorkloadViewModel
                {
                    TeacherId = teacher.TeacherId,
                    TeacherName = teacher.TeacherName,
                    SubjectName = teacher.Subject.SubjectName,
                    TotalPeriods = schedules.Count,
                    NumberOfClasses = schedules.Select(s => s.ClassId).Distinct().Count(),
                    IsHeadOfDepartment = teacher.IsHeadOfDepartment,
                    IsSupervisor = teacher.IsSupervisor,
                    Notes = teacher.Notes ?? ""
                };

                result.Add(workload);
            }

            return result.OrderBy(t => t.SubjectName).ThenBy(t => t.TeacherName).ToList();
        }

        // الحصول على الإعدادات الحالية
        private async Task<SchoolSetting> GetCurrentSettings()
        {
            return await _context.SchoolSettings.FirstAsync();
        }

        // البحث في الجدول
        public async Task<List<Schedule>> SearchSchedule(ScheduleSearchViewModel model)
        {
            var query = _context.Schedules
                .Include(s => s.Class)
                .Include(s => s.Subject)
                .Include(s => s.Teacher)
                .Include(s => s.SchoolDay)
                .Include(s => s.Period)
                .AsQueryable();

            if (model.ClassId.HasValue)
                query = query.Where(s => s.ClassId == model.ClassId.Value);

            if (model.SubjectId.HasValue)
                query = query.Where(s => s.SubjectId == model.SubjectId.Value);

            if (model.TeacherId.HasValue)
                query = query.Where(s => s.TeacherId == model.TeacherId.Value);

            if (model.DayId.HasValue)
                query = query.Where(s => s.DayId == model.DayId.Value);

            if (model.PeriodId.HasValue)
                query = query.Where(s => s.PeriodId == model.PeriodId.Value);

            if (!string.IsNullOrEmpty(model.AcademicYear))
                query = query.Where(s => s.AcademicYear == model.AcademicYear);

            if (model.Semester.HasValue)
                query = query.Where(s => s.Semester == model.Semester.Value);

            return await query
                .OrderBy(s => s.SchoolDay.DayOrder)
                .ThenBy(s => s.Period.PeriodNumber)
                .ToListAsync();
        }
    }
}
