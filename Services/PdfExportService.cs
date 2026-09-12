using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using iText.Kernel.Font;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using SchoolSchedule.Data;
using SchoolSchedule.Models;
using Microsoft.EntityFrameworkCore;

namespace SchoolSchedule.Services
{
    public class PdfExportService
    {
        private readonly ApplicationDbContext _context;
        private readonly ScheduleService _scheduleService;

        public PdfExportService(ApplicationDbContext context, ScheduleService scheduleService)
        {
            _context = context;
            _scheduleService = scheduleService;
        }

        public async Task<byte[]> ExportAllClassSchedulesToPdf(string academicYear, int semester)
        {
            byte[] result;
            
            using (var memoryStream = new MemoryStream())
            {
                var writer = new PdfWriter(memoryStream);
                writer.SetCloseStream(false); // مهم جداً!
                
                var pdf = new PdfDocument(writer);
                var document = new Document(pdf);

                // إعداد الخط - استخدام خط بسيط أولاً
                PdfFont regularFont;
                PdfFont boldFont;
                
                try
                {
                    // استخدام الخطوط الافتراضية فقط لتجنب المشاكل
                    regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                    boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                }
                catch (Exception)
                {
                    // في حالة أي خطأ، استخدم الخطوط الافتراضية
                    regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);
                    boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
                }

                document.SetFont(regularFont);

                // الحصول على إعدادات المدرسة
                var settings = await _context.SchoolSettings.FirstOrDefaultAsync();
                if (settings == null)
                {
                    throw new Exception("لم يتم العثور على إعدادات المدرسة");
                }
                
                // الحصول على جميع الصفوف
                var classes = await _context.Classes
                    .OrderBy(c => c.Grade)
                    .ThenBy(c => c.Section)
                    .ToListAsync();

                if (!classes.Any())
                {
                    throw new Exception("لا توجد صفوف في قاعدة البيانات");
                }

                // لكل صف، أضف صفحة جديدة
                bool isFirstClass = true;
                foreach (var classEntity in classes)
                {
                    try
                    {
                        // إضافة صفحة جديدة لكل صف (ماعدا الأول)
                        if (!isFirstClass)
                        {
                            document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                        }
                        isFirstClass = false;

                        // الحصول على جدول الصف
                        var viewModel = await _scheduleService.GetClassSchedule(
                            classEntity.ClassId, 
                            academicYear, 
                            semester);

                        if (viewModel == null)
                        {
                            continue;
                        }

                        // عنوان الصفحة
                        var title = new Paragraph(settings.SchoolName ?? "مدرسة")
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontSize(16)
                            .SetFont(boldFont);
                        document.Add(title);

                        var subtitle = new Paragraph("School Schedule")
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontSize(14)
                            .SetFont(boldFont);
                        document.Add(subtitle);

                        var classTitle = new Paragraph($"Class: {classEntity.ClassName}")
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontSize(12)
                            .SetFont(boldFont);
                        document.Add(classTitle);

                        var yearInfo = new Paragraph($"Year: {academicYear} - Semester {semester}")
                            .SetTextAlignment(TextAlignment.CENTER)
                            .SetFontSize(10)
                            .SetFont(regularFont);
                        document.Add(yearInfo);

                        document.Add(new Paragraph("\n").SetFont(regularFont));

                        // إنشاء الجدول
                        int columnCount = viewModel.DayNames.Count + 1;
                        float[] columnWidths = new float[columnCount];
                        columnWidths[0] = 2f;
                        for (int i = 1; i < columnCount; i++)
                        {
                            columnWidths[i] = 3f;
                        }
                        
                        var table = new Table(UnitValue.CreatePercentArray(columnWidths));
                        table.SetWidth(UnitValue.CreatePercentValue(100));

                        // ترويسة الجدول
                        table.AddHeaderCell(CreateHeaderCell("Period", boldFont));
                        foreach (var day in viewModel.DayNames)
                        {
                            table.AddHeaderCell(CreateHeaderCell(day, boldFont));
                        }

                        // صفوف الحصص
                        for (int periodIndex = 0; periodIndex < viewModel.PeriodNumbers.Count; periodIndex++)
                        {
                            var periodCell = new Cell()
                                .Add(new Paragraph($"Period {viewModel.PeriodNumbers[periodIndex]}")
                                    .SetFont(boldFont)
                                    .SetTextAlignment(TextAlignment.CENTER)
                                    .SetFontSize(10))
                                .Add(new Paragraph(viewModel.PeriodTimes[periodIndex])
                                    .SetFont(regularFont)
                                    .SetFontSize(8)
                                    .SetTextAlignment(TextAlignment.CENTER))
                                .SetBackgroundColor(ColorConstants.LIGHT_GRAY);
                            table.AddCell(periodCell);

                            for (int dayIndex = 0; dayIndex < viewModel.DayNames.Count; dayIndex++)
                            {
                                var schedule = viewModel.ScheduleGridList[dayIndex][periodIndex];
                                
                                if (schedule != null && !schedule.IsEmpty)
                                {
                                    var cell = new Cell()
                                        .Add(new Paragraph(schedule.SubjectName ?? "")
                                            .SetFont(boldFont)
                                            .SetFontSize(9)
                                            .SetTextAlignment(TextAlignment.CENTER))
                                        .Add(new Paragraph(schedule.TeacherName ?? "")
                                            .SetFont(regularFont)
                                            .SetFontSize(8)
                                            .SetTextAlignment(TextAlignment.CENTER))
                                        .SetBackgroundColor(new DeviceRgb(255, 243, 205));
                                    
                                    table.AddCell(cell);
                                }
                                else
                                {
                                    var emptyCell = new Cell()
                                        .Add(new Paragraph("-")
                                            .SetFont(regularFont)
                                            .SetTextAlignment(TextAlignment.CENTER)
                                            .SetFontColor(ColorConstants.GRAY));
                                    table.AddCell(emptyCell);
                                }
                            }
                        }

                        document.Add(table);

                        var footer = new Paragraph($"Date: {DateTime.Now:dd/MM/yyyy}")
                            .SetTextAlignment(TextAlignment.LEFT)
                            .SetFontSize(8)
                            .SetFont(regularFont)
                            .SetFontColor(ColorConstants.GRAY);
                        document.Add(footer);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error for class {classEntity.ClassName}: {ex.Message}");
                    }
                }

                document.Close();
                result = memoryStream.ToArray();
            }
            
            return result;
        }

        private Cell CreateHeaderCell(string text, PdfFont boldFont)
        {
            return new Cell()
                .Add(new Paragraph(text ?? "")
                    .SetFont(boldFont)
                    .SetTextAlignment(TextAlignment.CENTER)
                    .SetFontColor(ColorConstants.WHITE)
                    .SetFontSize(10))
                .SetBackgroundColor(new DeviceRgb(13, 110, 253));
        }
    }
}
