using MockviewAI.Models.DTOs;

namespace MockviewAI.Services.CvParser
{
    /// <summary>
    /// [USER CV] Làm sạch dữ liệu CV (từ AI hoặc từ form): cắt độ dài, bỏ mục rỗng / trùng.
    /// </summary>
    public static class CvDataSanitizer
    {
        #region Limits
        private const int MaxName = 100;
        private const int MaxTitle = 120;
        private const int MaxObjective = 1500;
        private const int MaxShort = 150;
        private const int MaxLong = 1500;
        private const int MaxSkill = 60;
        private const int MaxSkills = 40;
        private const int MaxItems = 10;
        #endregion

        #region Public
        public static CvParsedDataDto Sanitize(CvParsedDataDto? data)
        {
            var d = data ?? new CvParsedDataDto();

            d.FullName = Clip(d.FullName, MaxName);
            d.TargetJobTitle = Clip(d.TargetJobTitle, MaxTitle);
            d.CareerObjective = Clip(d.CareerObjective, MaxObjective);
            d.TotalYearsOfExperience = double.IsFinite(d.TotalYearsOfExperience)
                ? Math.Clamp(Math.Round(d.TotalYearsOfExperience, 1), 0, 50)
                : 0;

            d.HardSkills = CleanList(d.HardSkills, MaxSkills, MaxSkill);
            d.SoftSkills = CleanList(d.SoftSkills, MaxSkills, MaxSkill);

            d.WorkExperiences = (d.WorkExperiences ?? new())
                .Where(w => w != null)
                .Select(w => new CvWorkExperienceDto
                {
                    CompanyName = Clip(w.CompanyName, MaxShort),
                    JobTitle = Clip(w.JobTitle, MaxShort),
                    Duration = Clip(w.Duration, 60),
                    MainResponsibilities = Clip(w.MainResponsibilities, MaxLong),
                    Achievements = CleanList(w.Achievements, 10, 300)
                })
                .Where(w => w.CompanyName.Length > 0 || w.JobTitle.Length > 0)
                .Take(MaxItems)
                .ToList();

            d.Projects = (d.Projects ?? new())
                .Where(p => p != null)
                .Select(p => new CvProjectDto
                {
                    Name = Clip(p.Name, MaxShort),
                    Context = Clip(p.Context, MaxShort),
                    Role = Clip(p.Role, MaxShort),
                    Duration = Clip(p.Duration, 60),
                    Description = Clip(p.Description, MaxLong),
                    Responsibilities = CleanList(p.Responsibilities, 10, 500),
                    Technologies = CleanList(p.Technologies, 20, MaxSkill)
                })
                .Where(p => p.Name.Length > 0)
                .Take(MaxItems)
                .ToList();

            d.Educations = (d.Educations ?? new())
                .Where(e => e != null)
                .Select(e => new CvEducationDto
                {
                    SchoolName = Clip(e.SchoolName, MaxShort),
                    Major = Clip(e.Major, MaxShort),
                    Degree = Clip(e.Degree, MaxShort),
                    Gpa = NullIfEmpty(Clip(e.Gpa, 20))
                })
                .Where(e => e.SchoolName.Length > 0)
                .Take(MaxItems)
                .ToList();

            d.CertificationsAndAwards = (d.CertificationsAndAwards ?? new())
                .Where(c => c != null)
                .Select(c => new CvCertificationDto
                {
                    Title = Clip(c.Title, MaxShort),
                    Issuer = Clip(c.Issuer, MaxShort),
                    Year = NullIfEmpty(Clip(c.Year, 20))
                })
                .Where(c => c.Title.Length > 0)
                .Take(MaxItems)
                .ToList();

            return d;
        }

        /// <summary>Tách "C#, ASP.NET Core; SQL" thành danh sách kỹ năng.</summary>
        public static List<string> SplitSkills(string? csv) =>
            CleanList((csv ?? string.Empty).Split(new[] { ',', ';', '\n' }, StringSplitOptions.RemoveEmptyEntries).ToList(), MaxSkills, MaxSkill);

        public static bool IsEmpty(CvParsedDataDto d) =>
            d.FullName.Length == 0 && d.HardSkills.Count == 0 && d.WorkExperiences.Count == 0
            && d.Projects.Count == 0 && d.Educations.Count == 0;

        public static string Clip(string? text, int max)
        {
            var value = (text ?? string.Empty).Trim();
            return value.Length > max ? value[..max].TrimEnd() : value;
        }
        #endregion

        #region Helpers
        private static List<string> CleanList(List<string>? items, int maxCount, int maxLength) =>
            (items ?? new())
                .Select(i => Clip(i, maxLength))
                .Where(i => i.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(maxCount)
                .ToList();

        private static string? NullIfEmpty(string value) => value.Length == 0 ? null : value;
        #endregion
    }
}
