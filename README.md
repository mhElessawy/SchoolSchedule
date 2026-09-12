# نظام إدارة الجدول المدرسي
## School Schedule Management System

نظام متكامل لإدارة الجداول المدرسية باستخدام ASP.NET Core 8 MVC و SQL Server

---

## المميزات الرئيسية

### 1. إدارة الجداول
- ✅ عرض جدول كل صف دراسي
- ✅ عرض جدول كل معلم
- ✅ إضافة وتعديل وحذف الحصص
- ✅ منع التعارض التلقائي (معلم أو صف في نفس الوقت)

### 2. استيراد وتصدير
- ✅ استيراد البيانات من Excel
- ✅ تصدير الجداول إلى Excel
- ✅ دعم الملفات العربية

### 3. الإحصائيات
- ✅ إحصائيات عدد الحصص لكل معلم
- ✅ توزيع المعلمين على المواد
- ✅ عدد الصفوف والشعب

### 4. الطباعة
- ✅ طباعة جدول الصف
- ✅ طباعة جدول المعلم
- ✅ تصميم جاهز للطباعة

---

## متطلبات التشغيل

### البرامج المطلوبة
1. **Visual Studio 2022** (أو أحدث)
2. **SQL Server 2019** (أو أحدث) أو SQL Server Express
3. **.NET 8.0 SDK**

### الحزم المطلوبة (NuGet Packages)
```xml
- Microsoft.EntityFrameworkCore (8.0.0)
- Microsoft.EntityFrameworkCore.SqlServer (8.0.0)
- Microsoft.EntityFrameworkCore.Tools (8.0.0)
- EPPlus (7.0.5)
- Microsoft.AspNetCore.Session (2.2.0)
```

---

## خطوات التثبيت

### 1. إعداد قاعدة البيانات

#### الطريقة الأولى: باستخدام SQL Script
```sql
-- افتح SQL Server Management Studio
-- شغّل ملف: Database/SchoolScheduleDB.sql
```

#### الطريقة الثانية: باستخدام Entity Framework Migrations
```bash
# في Package Manager Console
Add-Migration InitialCreate
Update-Database
```

### 2. تكوين الاتصال بقاعدة البيانات
افتح ملف `appsettings.json` وعدّل connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=.;Database=SchoolScheduleDB;Trusted_Connection=True;TrustServerCertificate=True"
  }
}
```

**ملاحظة:** 
- إذا كنت تستخدم SQL Server Express: `Server=.\\SQLEXPRESS`
- إذا كنت تستخدم SQL Server Authentication: أضف `User Id=sa;Password=yourpassword`

### 3. تشغيل المشروع
1. افتح المشروع في Visual Studio 2022
2. اضغط F5 أو Ctrl+F5 للتشغيل
3. سيفتح المتصفح على: `https://localhost:xxxx`

---

## هيكل المشروع

```
SchoolSchedule/
│
├── Controllers/
│   ├── ScheduleController.cs     # التحكم في الجداول
│   └── ImportController.cs       # استيراد وتصدير Excel
│
├── Models/
│   ├── Models.cs                 # نماذج البيانات الأساسية
│   └── ViewModels.cs             # نماذج العرض
│
├── Data/
│   └── ApplicationDbContext.cs   # سياق قاعدة البيانات
│
├── Services/
│   ├── ScheduleService.cs        # خدمات إدارة الجداول
│   └── ExcelImportService.cs     # خدمات Excel
│
├── Views/
│   ├── Schedule/                 # صفحات الجداول
│   ├── Import/                   # صفحات الاستيراد
│   └── Shared/                   # القوالب المشتركة
│
└── Database/
    └── SchoolScheduleDB.sql      # سكريبت قاعدة البيانات
```

---

## الاستخدام

### 1. عرض جدول صف
```
الصفحة الرئيسية > قائمة الصفوف > اختر الصف
```

### 2. إضافة حصة جديدة
```
جدول الصف > اضغط على خلية فارغة > أدخل البيانات > حفظ
```

### 3. استيراد من Excel
```
القائمة > Excel > استيراد > اختر الملف > رفع
```

**تنسيق ملف Excel المطلوب:**
- يجب أن تحتوي على ورقة "جدول المدرسي"
- الأرقام مثل 81 تعني: صف 8 شعبة 1
- المواد يجب أن تكون موجودة في قاعدة البيانات

### 4. تصدير إلى Excel
```
القائمة > Excel > تصدير
```

---

## قاعدة البيانات

### الجداول الرئيسية

#### 1. Subjects (المواد)
- SubjectId, SubjectName, SubjectNameEn, IsActive

#### 2. Teachers (المعلمين)
- TeacherId, TeacherName, SubjectId, IsHeadOfDepartment, IsSupervisor, Notes

#### 3. Classes (الصفوف)
- ClassId, Grade, Section, ClassName (Computed), ClassCode (Computed)

#### 4. SchoolDays (الأيام)
- DayId, DayName, DayNameEn, DayOrder

#### 5. Periods (الحصص)
- PeriodId, PeriodNumber, StartTime, EndTime

#### 6. Schedule (الجدول المدرسي)
- ScheduleId, ClassId, SubjectId, TeacherId, DayId, PeriodId, AcademicYear, Semester

#### 7. SchoolSettings (إعدادات المدرسة)
- SettingId, SchoolName, EducationalZone, CurrentAcademicYear, CurrentSemester

### Stored Procedures
- GetClassSchedule
- GetTeacherSchedule
- GetTeacherWorkload
- CheckScheduleConflict

---

## التخصيص

### تغيير اسم المدرسة
```sql
UPDATE SchoolSettings 
SET SchoolName = N'اسم المدرسة الجديد'
WHERE SettingId = 1
```

### إضافة مواد جديدة
```sql
INSERT INTO Subjects (SubjectName, SubjectNameEn)
VALUES (N'المادة الجديدة', 'New Subject')
```

### تغيير عدد الحصص
```sql
UPDATE SchoolSettings 
SET PeriodsPerDay = 8
WHERE SettingId = 1

-- ثم أضف الحصة الجديدة
INSERT INTO Periods (PeriodNumber, StartTime, EndTime)
VALUES (8, '13:00', '13:45')
```

---

## استكشاف الأخطاء

### خطأ في الاتصال بقاعدة البيانات
```
✅ تأكد من تشغيل SQL Server
✅ راجع connection string في appsettings.json
✅ تأكد من وجود قاعدة البيانات SchoolScheduleDB
```

### خطأ في استيراد Excel
```
✅ تأكد من تثبيت حزمة EPPlus
✅ تأكد من صيغة الملف (.xlsx أو .xls)
✅ تأكد من وجود ورقة "جدول المدرسي"
```

### تعارض في الحصص
```
النظام يمنع التعارض تلقائياً:
- المعلم لا يمكن أن يكون في مكانين في نفس الوقت
- الصف لا يمكن أن يكون له حصتين في نفس الوقت
```

---

## الميزات المستقبلية

- [ ] نظام صلاحيات المستخدمين
- [ ] إشعارات التعديلات
- [ ] تطبيق موبايل
- [ ] تقارير متقدمة بصيغة PDF
- [ ] نسخ احتياطي تلقائي
- [ ] دعم أكثر من مدرسة

---

## الدعم الفني

للاستفسارات والدعم:
- البريد الإلكتروني: support@school.com
- الهاتف: +965-XXXX-XXXX

---

## الترخيص

هذا المشروع مفتوح المصدر ويمكن استخدامه وتعديله بحرية.

---

## المطور

تم تطوير النظام باستخدام:
- ASP.NET Core 8.0 MVC
- Entity Framework Core 8.0
- SQL Server
- Bootstrap 5 RTL
- Font Awesome 6
- EPPlus (Excel)

**تاريخ الإنشاء:** يناير 2026

---

## نصائح مهمة

1. **النسخ الاحتياطي**: قم بعمل نسخة احتياطية من قاعدة البيانات بانتظام
2. **الأمان**: استخدم نظام مصادقة قوي في بيئة الإنتاج
3. **الأداء**: راقب أداء قاعدة البيانات مع زيادة البيانات
4. **التحديثات**: تابع تحديثات الحزم والأمان

---

## شكر خاص

شكراً لاستخدامك نظام إدارة الجدول المدرسي!

نتمنى أن يساعدك هذا النظام في تنظيم الجداول المدرسية بكفاءة.
