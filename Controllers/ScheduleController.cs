using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SchoolSchedule.Data;
using SchoolSchedule.Models;
using SchoolSchedule.Models.ViewModels;
using SchoolSchedule.Services;

namespace SchoolSchedule.Controllers
{
    public class ScheduleController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ScheduleService _scheduleService;
        private readonly PdfExportService _pdfExportService;

        public ScheduleController(ApplicationDbContext context, ScheduleService scheduleService, PdfExportService pdfExportService)
        {
            _context = context;
            _scheduleService = scheduleService;
            _pdfExportService = pdfExportService;
        }

        // الصفحة الرئيسية
        public async Task<IActionResult> Index()
        {
            var settings = await _context.SchoolSettings.FirstAsync();
            ViewBag.AcademicYear = settings.CurrentAcademicYear;
            ViewBag.Semester = settings.CurrentSemester;
            ViewBag.SchoolName = settings.SchoolName;

            return View();
        }

        // عرض جدول صف
        public async Task<IActionResult> ClassSchedule(int id)
        {
            try
            {
                var viewModel = await _scheduleService.GetClassSchedule(id);
                return View(viewModel);
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // عرض جدول معلم
        public async Task<IActionResult> TeacherSchedule(int id)
        {
            try
            {
                var viewModel = await _scheduleService.GetTeacherSchedule(id);
                return View(viewModel);
            }
            catch (ArgumentException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }

        // إنشاء حصة جديدة - GET
        public async Task<IActionResult> Create(int? classId, int? dayId, int? periodId)
        {
            var settings = await _context.SchoolSettings.FirstAsync();

            var model = new ScheduleFormViewModel
            {
                AcademicYear = settings.CurrentAcademicYear,
                Semester = settings.CurrentSemester,
                ClassId = classId ?? 0,
                DayId = dayId ?? 0,
                PeriodId = periodId ?? 0
            };

            await LoadDropDowns();
            return View(model);
        }

        // إنشاء حصة جديدة - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ScheduleFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var (success, message) = await _scheduleService.AddSchedule(model);

                if (success)
                {
                    TempData["Success"] = message;
                    return RedirectToAction(nameof(ClassSchedule), new { id = model.ClassId });
                }
                else
                {
                    ModelState.AddModelError("", message);
                }
            }

            await LoadDropDowns();
            return View(model);
        }

        // تعديل حصة - GET
        public async Task<IActionResult> Edit(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Class)
                .Include(s => s.Subject)
                .Include(s => s.Teacher)
                .Include(s => s.SchoolDay)
                .Include(s => s.Period)
                .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (schedule == null)
            {
                TempData["Error"] = "الحصة غير موجودة";
                return RedirectToAction(nameof(Index));
            }

            var model = new ScheduleFormViewModel
            {
                ScheduleId = schedule.ScheduleId,
                ClassId = schedule.ClassId,
                SubjectId = schedule.SubjectId,
                TeacherId = schedule.TeacherId,
                DayId = schedule.DayId,
                PeriodId = schedule.PeriodId,
                AcademicYear = schedule.AcademicYear,
                Semester = schedule.Semester,
                Notes = schedule.Notes,
                ClassName = schedule.Class.ClassName,
                SubjectName = schedule.Subject.SubjectName,
                TeacherName = schedule.Teacher.TeacherName,
                DayName = schedule.SchoolDay.DayName,
                PeriodNumber = schedule.Period.PeriodNumber
            };

            await LoadDropDowns(model.SubjectId);
            return View(model);
        }

        // تعديل حصة - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ScheduleFormViewModel model)
        {
            if (ModelState.IsValid)
            {
                var (success, message) = await _scheduleService.UpdateSchedule(model);

                if (success)
                {
                    TempData["Success"] = message;
                    return RedirectToAction(nameof(ClassSchedule), new { id = model.ClassId });
                }
                else
                {
                    ModelState.AddModelError("", message);
                }
            }

            await LoadDropDowns(model.SubjectId);
            return View(model);
        }

        // حذف حصة - GET
        public async Task<IActionResult> Delete(int id)
        {
            var schedule = await _context.Schedules
                .Include(s => s.Class)
                .Include(s => s.Subject)
                .Include(s => s.Teacher)
                .Include(s => s.SchoolDay)
                .Include(s => s.Period)
                .FirstOrDefaultAsync(s => s.ScheduleId == id);

            if (schedule == null)
            {
                TempData["Error"] = "الحصة غير موجودة";
                return RedirectToAction(nameof(Index));
            }

            return View(schedule);
        }

        // حذف حصة - POST
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var schedule = await _context.Schedules.FindAsync(id);
            if (schedule == null)
            {
                TempData["Error"] = "الحصة غير موجودة";
                return RedirectToAction(nameof(Index));
            }

            var classId = schedule.ClassId;
            var success = await _scheduleService.DeleteSchedule(id);

            if (success)
            {
                TempData["Success"] = "تم حذف الحصة بنجاح";
            }

            return RedirectToAction(nameof(ClassSchedule), new { id = classId });
        }

        // نسخ سريع - GET
        public async Task<IActionResult> QuickCopy()
        {
            var settings = await _context.SchoolSettings.FirstAsync();

            var model = new QuickCopyViewModel
            {
                AcademicYear = settings.CurrentAcademicYear,
                Semester = settings.CurrentSemester
            };

            await LoadClassesDropDown();
            return View(model);
        }

        // نسخ سريع - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> QuickCopy(QuickCopyViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.FromClassId == model.ToClassId)
                {
                    ModelState.AddModelError("", "لا يمكن نسخ الجدول لنفس الصف");
                }
                else
                {
                    var (success, message, copiedCount) = await _scheduleService.CopySchedule(model);

                    if (success)
                    {
                        TempData["Success"] = message;
                        return RedirectToAction(nameof(ClassSchedule), new { id = model.ToClassId });
                    }
                    else
                    {
                        ModelState.AddModelError("", message);
                    }
                }
            }

            await LoadClassesDropDown();
            return View(model);
        }

        // إحصائيات المعلمين
        public async Task<IActionResult> TeacherWorkloads()
        {
            var workloads = await _scheduleService.GetTeacherWorkloads();
            
            var settings = await _context.SchoolSettings.FirstAsync();
            ViewBag.AcademicYear = settings.CurrentAcademicYear;
            ViewBag.Semester = settings.CurrentSemester;

            return View(workloads);
        }

        // البحث
        public async Task<IActionResult> Search()
        {
            var model = new ScheduleSearchViewModel();
            await LoadSearchDropDowns();
            return View(model);
        }

        // البحث - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(ScheduleSearchViewModel model)
        {
            model.Results = await _scheduleService.SearchSchedule(model);
            await LoadSearchDropDowns();
            return View(model);
        }

        // قائمة الصفوف
        public async Task<IActionResult> ClassList()
        {
            var classes = await _context.Classes
                .Where(c => c.IsActive)
                .OrderBy(c => c.Grade)
                .ThenBy(c => c.Section)
                .ToListAsync();

            return View(classes);
        }

        // قائمة المعلمين
        public async Task<IActionResult> TeacherList()
        {
            var teachers = await _context.Teachers
                .Include(t => t.Subject)
                .Where(t => t.IsActive)
                .OrderBy(t => t.Subject.SubjectName)
                .ThenBy(t => t.TeacherName)
                .ToListAsync();

            return View(teachers);
        }

        // طباعة جدول صف
        public async Task<IActionResult> PrintClassSchedule(int id)
        {
            var viewModel = await _scheduleService.GetClassSchedule(id);
            var settings = await _context.SchoolSettings.FirstAsync();
            
            ViewBag.SchoolName = settings.SchoolName;
            ViewBag.EducationalZone = settings.EducationalZone;

            return View(viewModel);
        }

        // طباعة جدول معلم
        public async Task<IActionResult> PrintTeacherSchedule(int id)
        {
            var viewModel = await _scheduleService.GetTeacherSchedule(id);
            var settings = await _context.SchoolSettings.FirstAsync();
            
            ViewBag.SchoolName = settings.SchoolName;
            ViewBag.EducationalZone = settings.EducationalZone;

            return View(viewModel);
        }

        // AJAX - الحصول على المعلمين حسب المادة
        [HttpGet]
        public async Task<JsonResult> GetTeachersBySubject(int subjectId)
        {
            var teachers = await _context.Teachers
                .Where(t => t.SubjectId == subjectId && t.IsActive)
                .Select(t => new { value = t.TeacherId, text = t.TeacherName })
                .ToListAsync();

            return Json(teachers);
        }

        // Helper Methods
        private async Task LoadDropDowns(int? selectedSubjectId = null)
        {
            ViewBag.Classes = new SelectList(
                await _context.Classes.OrderBy(c => c.Grade).ThenBy(c => c.Section).ToListAsync(),
                "ClassId", "ClassName");

            ViewBag.Subjects = new SelectList(
                await _context.Subjects.Where(s => s.IsActive).ToListAsync(),
                "SubjectId", "SubjectName", selectedSubjectId);

            if (selectedSubjectId.HasValue)
            {
                ViewBag.Teachers = new SelectList(
                    await _context.Teachers
                        .Where(t => t.SubjectId == selectedSubjectId.Value && t.IsActive)
                        .ToListAsync(),
                    "TeacherId", "TeacherName");
            }
            else
            {
                ViewBag.Teachers = new SelectList(Enumerable.Empty<SelectListItem>());
            }

            ViewBag.Days = new SelectList(
                await _context.SchoolDays.OrderBy(d => d.DayOrder).ToListAsync(),
                "DayId", "DayName");

            ViewBag.Periods = new SelectList(
                await _context.Periods.OrderBy(p => p.PeriodNumber).ToListAsync(),
                "PeriodId", "PeriodNumber");
        }

        private async Task LoadClassesDropDown()
        {
            ViewBag.Classes = new SelectList(
                await _context.Classes.OrderBy(c => c.Grade).ThenBy(c => c.Section).ToListAsync(),
                "ClassId", "ClassName");
        }

        private async Task LoadSearchDropDowns()
        {
            ViewBag.Classes = new SelectList(
                await _context.Classes.OrderBy(c => c.Grade).ThenBy(c => c.Section).ToListAsync(),
                "ClassId", "ClassName");

            ViewBag.Subjects = new SelectList(
                await _context.Subjects.Where(s => s.IsActive).ToListAsync(),
                "SubjectId", "SubjectName");

            ViewBag.Teachers = new SelectList(
                await _context.Teachers.Where(t => t.IsActive).ToListAsync(),
                "TeacherId", "TeacherName");

            ViewBag.Days = new SelectList(
                await _context.SchoolDays.OrderBy(d => d.DayOrder).ToListAsync(),
                "DayId", "DayName");

            ViewBag.Periods = new SelectList(
                await _context.Periods.OrderBy(p => p.PeriodNumber).ToListAsync(),
                "PeriodId", "PeriodNumber");
        }

        // عرض جميع جداول الصفوف في صفحة واحدة
        [HttpGet]
        public async Task<IActionResult> AllClassesSchedules()
        {
            try
            {
                var settings = await _context.SchoolSettings.FirstAsync();
                var academicYear = settings.CurrentAcademicYear;
                var semester = settings.CurrentSemester;

                // الحصول على جميع الصفوف
                var classes = await _context.Classes
                    .OrderBy(c => c.Grade)
                    .ThenBy(c => c.Section)
                    .ToListAsync();

                // الحصول على جدول كل صف
                var schedules = new List<ClassScheduleViewModel>();
                
                foreach (var classEntity in classes)
                {
                    var viewModel = await _scheduleService.GetClassSchedule(
                        classEntity.ClassId, 
                        academicYear, 
                        semester);
                    
                    if (viewModel != null)
                    {
                        schedules.Add(viewModel);
                    }
                }

                ViewBag.SchoolName = settings.SchoolName;
                ViewBag.AcademicYear = academicYear;
                ViewBag.Semester = semester;

                return View(schedules);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ: {ex.Message}";
                return RedirectToAction("ClassList");
            }
        }

        // تصدير جداول جميع الصفوف إلى PDF
        [HttpGet]
        public async Task<IActionResult> ExportAllClassesToPdf()
        {
            try
            {
                var settings = await _context.SchoolSettings.FirstAsync();
                var academicYear = settings.CurrentAcademicYear;
                var semester = settings.CurrentSemester;

                var pdfBytes = await _pdfExportService.ExportAllClassSchedulesToPdf(
                    academicYear,
                    (int)semester);

                var fileName = $"جداول_الصفوف_{academicYear.Replace("/", "-")}_الفصل{semester}.pdf";
                
                return File(pdfBytes, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ أثناء التصدير: {ex.Message}";
                return RedirectToAction("ClassList");
            }
        }
    }
}
