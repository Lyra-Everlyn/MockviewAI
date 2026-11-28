using MockviewAI.Models.InterviewQuestions;

namespace MockviewAI.Services.InterviewQuestions
{
    /// <summary>
    /// [IT-ROLES]
    /// Danh mục 15 vị trí ngành IT. Giữ 3 khoá cũ (developer, tester, ba) để buổi cũ và form cũ vẫn chạy.
    /// Thêm vị trí = thêm 1 dòng ở đây (không cần đổi DB vì InterviewSession.Position lưu tên).
    /// </summary>
    public static class ItRoleCatalog
    {
        public const string GroupSoftware = "Phát triển phần mềm";
        public const string GroupInfraData = "Hạ tầng & dữ liệu";
        public const string GroupQualityBusiness = "Chất lượng & nghiệp vụ";
        public const string GroupDesign = "Thiết kế";

        public static readonly IReadOnlyList<ItRole> All = new List<ItRole>
        {
            new() { Key = "developer", Name = "Lập trình viên", Group = GroupSoftware, BankBase = "developer", HasTechBank = true,
                    Focus = "lập trình hướng đối tượng, cấu trúc dữ liệu, cơ sở dữ liệu, API, Git",
                    Keywords = new[] { "lap trinh vien", "developer", "software engineer", "ky su phan mem" } },
            new() { Key = "backend", Name = "Lập trình viên Backend", Group = GroupSoftware, BankBase = "developer", HasTechBank = true,
                    Focus = "thiết kế API, cơ sở dữ liệu và truy vấn, xác thực / phân quyền, xử lý lỗi, hiệu năng phía server",
                    Keywords = new[] { "backend", "back end", "asp net", "spring", "node js", "nodejs", "django", "laravel", "api", "microservice" } },
            new() { Key = "fullstack", Name = "Lập trình viên Fullstack", Group = GroupSoftware, BankBase = "developer", HasTechBank = true,
                    Focus = "kết nối giao diện với API, cơ sở dữ liệu, xác thực, triển khai một ứng dụng web hoàn chỉnh",
                    Keywords = new[] { "fullstack", "full stack", "mern", "mean" } },
            new() { Key = "frontend", Name = "Lập trình viên Frontend", Group = GroupSoftware, BankBase = "developer",
                    Focus = "HTML/CSS/JavaScript, framework giao diện (React, Vue, Angular), responsive, quản lý trạng thái, gọi API, hiệu năng trang, khả năng truy cập",
                    Keywords = new[] { "frontend", "front end", "react", "reactjs", "vue", "angular", "next js", "html", "css", "tailwind", "typescript" } },
            new() { Key = "mobile", Name = "Lập trình viên Mobile", Group = GroupSoftware, BankBase = "developer",
                    Focus = "Android / iOS / Flutter / React Native, vòng đời màn hình, lưu dữ liệu cục bộ, quyền truy cập, hiệu năng và pin, phát hành lên store",
                    Keywords = new[] { "mobile", "android", "ios", "flutter", "react native", "kotlin", "swift", "dart" } },
            new() { Key = "game", Name = "Lập trình viên Game", Group = GroupSoftware, BankBase = "developer",
                    Focus = "vòng lặp game, xử lý input / sự kiện, va chạm và vật lý, quản lý trạng thái / scene, tối ưu khung hình, engine (Unity, Unreal, Godot) hoặc scripting",
                    Keywords = new[] { "game", "unity", "unreal", "godot", "lua", "gameplay", "map creator", "sandbox", "c++ game" } },
            new() { Key = "embedded", Name = "Kỹ sư Nhúng (Embedded)", Group = GroupSoftware, BankBase = "developer",
                    Focus = "vi điều khiển, C/C++, ngắt, giao tiếp ngoại vi (UART, I2C, SPI), bộ nhớ hạn chế, RTOS, IoT",
                    Keywords = new[] { "embedded", "nhung", "arduino", "esp32", "stm32", "vi dieu khien", "iot", "rtos", "firmware" } },
            new() { Key = "devops", Name = "DevOps / Cloud", Group = GroupInfraData, BankBase = "developer",
                    Focus = "CI/CD, Docker / container, Linux, dịch vụ cloud cơ bản, giám sát và log, triển khai / rollback, hạ tầng như mã",
                    Keywords = new[] { "devops", "docker", "kubernetes", "k8s", "ci cd", "jenkins", "github actions", "aws", "azure", "gcp", "terraform", "linux", "cloud" } },
            new() { Key = "data", Name = "Phân tích dữ liệu / BI", Group = GroupInfraData, BankBase = "developer",
                    Focus = "SQL, làm sạch dữ liệu, thống kê cơ bản, trực quan hoá / dashboard, ETL, diễn giải số liệu cho người ra quyết định",
                    Keywords = new[] { "data analyst", "phan tich du lieu", "power bi", "tableau", "excel", "bi", "etl", "data engineer", "pandas", "dashboard" } },
            new() { Key = "ai", Name = "AI / Machine Learning", Group = GroupInfraData, BankBase = "developer",
                    Focus = "tiền xử lý dữ liệu, chia train / validation / test, overfitting, đánh giá mô hình, chọn mô hình, đưa mô hình hoặc LLM vào sản phẩm",
                    Keywords = new[] { "machine learning", "deep learning", "ai engineer", "tensorflow", "pytorch", "scikit", "nlp", "computer vision", "llm" } },
            new() { Key = "security", Name = "An toàn thông tin (Bảo mật)", Group = GroupInfraData, BankBase = "developer",
                    Focus = "lỗ hổng web phổ biến (OWASP Top 10), xác thực / phân quyền, mã hoá và băm, quản lý bí mật, log và phát hiện xâm nhập, kiểm thử bảo mật",
                    Keywords = new[] { "security", "bao mat", "an toan thong tin", "pentest", "owasp", "ctf", "soc", "cyber" } },
            new() { Key = "tester", Name = "Kiểm thử viên (Tester)", Group = GroupQualityBusiness, BankBase = "tester", HasTechBank = true,
                    Focus = "thiết kế test case, kiểm thử thủ công / tự động, báo lỗi, kiểm thử API, quy trình Agile",
                    Keywords = new[] { "tester", "qa", "qc", "kiem thu", "test case", "selenium", "automation test", "postman" } },
            new() { Key = "ba", Name = "Chuyên viên Phân tích (BA)", Group = GroupQualityBusiness, BankBase = "ba", HasTechBank = true,
                    Focus = "thu thập và phân tích yêu cầu, user story, sơ đồ nghiệp vụ, làm việc với các bên liên quan",
                    Keywords = new[] { "business analyst", "phan tich nghiep vu", "user story", "srs", "bpmn", "yeu cau nghiep vu" } },
            new() { Key = "pm", Name = "Quản lý dự án / Product Owner", Group = GroupQualityBusiness, BankBase = "ba",
                    Focus = "phạm vi dự án, ưu tiên backlog, Scrum / sprint, ước lượng, rủi ro, giao tiếp với các bên liên quan, chỉ số thành công của sản phẩm",
                    Keywords = new[] { "project manager", "product owner", "product manager", "scrum master", "quan ly du an", "jira" } },
            new() { Key = "uiux", Name = "Thiết kế UI/UX", Group = GroupDesign, BankBase = "ba",
                    Focus = "nghiên cứu người dùng, wireframe / prototype, luồng người dùng, nguyên tắc thiết kế giao diện, kiểm thử khả dụng, phối hợp với lập trình viên",
                    Keywords = new[] { "ui ux", "ux", "ui designer", "figma", "wireframe", "prototype", "user research" } }
        };

        private static readonly Dictionary<string, ItRole> ByKey = All.ToDictionary(r => r.Key, StringComparer.Ordinal);

        /// <returns>null nếu khoá không có trong danh mục.</returns>
        public static ItRole? Find(string? key) => key != null && ByKey.TryGetValue(key, out var role) ? role : null;

        /// <summary>Các nhóm theo thứ tự hiển thị.</summary>
        public static IEnumerable<IGrouping<string, ItRole>> Groups => All.GroupBy(r => r.Group);
    }
}
