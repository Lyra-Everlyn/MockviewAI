namespace MockviewAI.Services.Admin
{
    /// <summary>
    /// [ADMIN] 
    /// Section "Admin": danh sách email được xem trang quản trị (Admin__Emails__0, Admin__Emails__1...).
    /// </summary>
    public class AdminOptions
    {
        public const string SectionName = "Admin";

        public string[] Emails { get; set; } = Array.Empty<string>();
    }
}
