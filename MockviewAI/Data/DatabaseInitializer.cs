// [BẢN TEST] Tự chuẩn bị database khi khởi động ứng dụng.
// Lý do: repo chưa có thư mục Migrations, nên Migrate() không làm gì cả. Database tạo từ trước
// sẽ thiếu cột mới (vd: LongestPauseSeconds) và làm trang Chi tiết buổi luyện bị lỗi.
// Cách làm: không có Migrations -> EnsureCreated() (tạo bảng nếu chưa có) rồi tự thêm cột còn thiếu.
// Khi thêm cột mới vào Entity: thêm 1 dòng vào AddedColumns bên dưới.

using System.Data;
using Microsoft.EntityFrameworkCore;

namespace MockviewAI.Data
{
    public static class DatabaseInitializer
    {
        // (Bảng, Cột, Kiểu dữ liệu MySQL) - chỉ thêm khi cột chưa tồn tại, an toàn khi chạy nhiều lần
        private static readonly (string Table, string Column, string Definition)[] AddedColumns =
        {
            ("InterviewSessions", "AnxietyBefore", "int NULL"),
            ("InterviewSessions", "AnxietyAfter", "int NULL"),
            ("SessionAnswers", "DurationSeconds", "double NULL"),
            ("SessionAnswers", "LongestPauseSeconds", "double NULL"),
            ("SessionAnswers", "Skipped", "tinyint(1) NOT NULL DEFAULT 0"),
            ("SessionAnswers", "Attempts", "int NOT NULL DEFAULT 0"),
            ("InterviewSessions", "AnxietyIndicator", "int NULL"),
            ("InterviewSessions", "EmailResult", "tinyint(1) NOT NULL DEFAULT 0"),
        };

        public static void Initialize(ApplicationDbContext db, ILogger logger)
        {
            try
            {
                if (db.Database.GetMigrations().Any())
                {
                    db.Database.Migrate();
                    return;
                }

                db.Database.EnsureCreated();
                AddMissingColumns(db, logger);
            }
            catch (Exception ex)
            {
                // Không để ứng dụng sập lúc khởi động: ghi log rõ nguyên nhân, các trang sẽ báo lỗi thân thiện
                logger.LogError(ex, "Không chuẩn bị được database. Kiểm tra Docker (MySQL) đang chạy và chuỗi kết nối DefaultConnection.");
            }
        }

        private static void AddMissingColumns(ApplicationDbContext db, ILogger logger)
        {
            foreach (var (table, column, definition) in AddedColumns)
            {
                if (Count(db, "SELECT COUNT(*) FROM information_schema.TABLES WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @t", ("@t", table)) == 0)
                    continue;

                if (Count(db, "SELECT COUNT(*) FROM information_schema.COLUMNS WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = @t AND COLUMN_NAME = @c", ("@t", table), ("@c", column)) > 0)
                    continue;

                db.Database.ExecuteSqlRaw("ALTER TABLE `" + table + "` ADD COLUMN `" + column + "` " + definition);
                logger.LogWarning("Đã tự thêm cột thiếu {Table}.{Column} vào database.", table, column);
            }
        }

        private static long Count(ApplicationDbContext db, string sql, params (string Name, object Value)[] args)
        {
            var connection = db.Database.GetDbConnection();
            bool wasClosed = connection.State != ConnectionState.Open;
            if (wasClosed) connection.Open();
            try
            {
                using var command = connection.CreateCommand();
                command.CommandText = sql;
                foreach (var (name, value) in args)
                {
                    var parameter = command.CreateParameter();
                    parameter.ParameterName = name;
                    parameter.Value = value;
                    command.Parameters.Add(parameter);
                }
                return Convert.ToInt64(command.ExecuteScalar());
            }
            finally
            {
                if (wasClosed) connection.Close();
            }
        }
    }
}
