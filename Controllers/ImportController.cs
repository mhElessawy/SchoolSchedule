using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SchoolSchedule.Data;
using SchoolSchedule.Models.ViewModels;
using SchoolSchedule.Services;

namespace SchoolSchedule.Controllers
{
    public class ImportController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ExcelImportService _importService;

        public ImportController(ApplicationDbContext context, ExcelImportService importService)
        {
            _context = context;
            _importService = importService;
        }

        // صفحة الاستيراد - GET
        public async Task<IActionResult> Import()
        {
            var settings = await _context.SchoolSettings.FirstAsync();

            var model = new ImportScheduleViewModel
            {
                AcademicYear = settings.CurrentAcademicYear!,
                Semester = settings.CurrentSemester!.Value,
                DeleteExisting = true
            };

            return View(model);
        }

        // استيراد البيانات - POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(ImportScheduleViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (model.ExcelFile == null || model.ExcelFile.Length == 0)
                {
                    ModelState.AddModelError("ExcelFile", "يرجى اختيار ملف Excel");
                    return View(model);
                }

                // التحقق من امتداد الملف
                var extension = Path.GetExtension(model.ExcelFile.FileName).ToLower();
                if (extension != ".xlsx" && extension != ".xls")
                {
                    ModelState.AddModelError("ExcelFile", "يجب أن يكون الملف بصيغة Excel (.xlsx أو .xls)");
                    return View(model);
                }

                try
                {
                    using (var stream = model.ExcelFile.OpenReadStream())
                    {
                        var result = await _importService.ImportScheduleFromExcel(
                            stream,
                            model.AcademicYear,
                            model.Semester,
                            model.DeleteExisting);

                        if (result.Success)
                        {
                            var settings = await _context.SchoolSettings.FirstAsync();
                            settings.CurrentAcademicYear = model.AcademicYear;
                            settings.CurrentSemester = model.Semester;
                            settings.UpdatedDate = DateTime.Now;
                            await _context.SaveChangesAsync();

                            TempData["Success"] = $"تم استيراد البيانات بنجاح! المعلمين الجدد: {result.TeachersAdded}، الحصص المضافة أو المحدثة: {result.SchedulesAdded}";

                            if (result.Warnings.Any())
                            {
                                TempData["Warnings"] = string.Join("<br/>", result.Warnings);
                            }

                            return RedirectToAction("Index", "Schedule");
                        }
                        else
                        {
                            foreach (var error in result.Errors)
                            {
                                ModelState.AddModelError("", error);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"حدث خطأ أثناء الاستيراد: {ex.Message}");
                }
            }

            return View(model);
        }

        // تصدير البيانات إلى Excel
        public async Task<IActionResult> Export(string? academicYear = null, int? semester = null)
        {
            try
            {
                var settings = await _context.SchoolSettings.FirstAsync();
                academicYear ??= settings.CurrentAcademicYear;
                semester ??= settings.CurrentSemester;

                var fileBytes = await _importService.ExportScheduleToExcel(academicYear!, semester!.Value);

                var fileName = $"جدول_مدرسي_{academicYear}_{semester}.xlsx";

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"حدث خطأ أثناء التصدير: {ex.Message}";
                return RedirectToAction("Index", "Schedule");
            }
        }

        // صفحة تنزيل نموذج Excel
        public IActionResult DownloadTemplate()
        {
            return View();
        }

        // تنزيل نموذج Excel فارغ
        public IActionResult GetTemplate()
        {
            // هنا يمكنك إنشاء ملف Excel فارغ كنموذج
            // أو توفير ملف موجود مسبقاً

            TempData["Info"] = "يمكنك استخدام الملف المرفوع كنموذج للاستيراد";
            return RedirectToAction(nameof(Import));
        }
    }
}