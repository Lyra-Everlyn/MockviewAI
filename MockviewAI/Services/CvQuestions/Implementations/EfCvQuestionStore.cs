using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MockviewAI.Data;
using MockviewAI.Models;
using MockviewAI.Models.CvQuestions;
using MockviewAI.Models.Entities;
using MockviewAI.Models.InterviewQuestions;
using MockviewAI.Services.CvQuestions.Interfaces;
using MockviewAI.Services.InterviewQuestions;

namespace MockviewAI.Services.CvQuestions.Implementations
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Lưu câu hỏi theo CV vào MySQL (EF Core). Câu hỏi lưu dạng JSON của BankQuestion để bộ chấm, hỏi thêm
    /// và trang Xem lại dùng lại y như câu bộ đề, kể cả khi Redis đã hết hạn.
    /// </summary>
    public class EfCvQuestionStore : ICvQuestionStore
    {
        #region Fields
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly ApplicationDbContext _db;
        private readonly ILogger<EfCvQuestionStore> _logger;
        #endregion

        #region Constructor
        public EfCvQuestionStore(ApplicationDbContext db, ILogger<EfCvQuestionStore> logger)
        {
            _db = db;
            _logger = logger;
        }
        #endregion

        #region ICvQuestionStore
        public async Task<List<BankQuestion>> GetForSessionAsync(int sessionId, int userId, CancellationToken ct = default)
        {
            var rows = await _db.CvGeneratedQuestions
                .AsNoTracking()
                .Where(q => q.SessionId == sessionId && q.Session!.UserId == userId)
                .Select(q => new { q.Id, q.PayloadJson })
                .ToListAsync(ct);

            var result = new List<BankQuestion>();
            foreach (var row in rows)
            {
                try
                {
                    var question = JsonSerializer.Deserialize<BankQuestion>(row.PayloadJson, JsonOptions);
                    if (question == null) continue;
                    question.Id = row.Id;   // mã trong DB là chuẩn
                    result.Add(question);
                }
                catch (JsonException)
                {
                    _logger.LogWarning("CV question {QuestionId} has unreadable payload, skipped", row.Id);
                }
            }
            return result;
        }

        public async Task<int> AttachToSessionAsync(InterviewSession session, CvQuestionPipelineResult result, CancellationToken ct = default)
        {
            if (session.Id <= 0 || result.Accepted.Count == 0) return 0;

            // Giữ câu 1 (giới thiệu), thay từ câu cuối ngược lên
            var targets = FollowUps.Main(session.Answers)
                .Where(a => a.OrderNo > 1)
                .OrderByDescending(a => a.OrderNo)
                .Take(result.Accepted.Count)
                .OrderBy(a => a.OrderNo)
                .ToList();
            if (targets.Count == 0) return 0;

            var originals = targets.Select(a => (a.Question, a.QuestionId)).ToList();
            var rows = new List<CvGeneratedQuestion>();
            for (int i = 0; i < targets.Count; i++)
            {
                var draft = result.Accepted[i];
                string id = $"{QuestionBankStoreExtensions.CvIdPrefix}{session.Id}-{i + 1}";
                draft.Question.Id = id;

                targets[i].Question = draft.Question.Question;
                targets[i].QuestionId = id;
                rows.Add(new CvGeneratedQuestion
                {
                    Id = id,
                    SessionId = session.Id,
                    PayloadJson = JsonSerializer.Serialize(draft.Question, JsonOptions),
                    ReviewVerdict = draft.ReviewVerdict,
                    ReviewNotes = Clip(draft.ReviewNotes, 1000),
                    GeneratorModel = Clip(result.GeneratorModel, 60) ?? string.Empty,
                    ReviewerModel = Clip(result.ReviewerModel, 60) ?? string.Empty
                });
            }

            _db.CvGeneratedQuestions.AddRange(rows);
            try
            {
                await _db.SaveChangesAsync(ct);
                return targets.Count;
            }
            catch (DbUpdateException ex)
            {
                // Không để câu "mồ côi" (có mã cv- nhưng không có đáp án): trả về câu bộ đề ban đầu
                _logger.LogWarning(ex, "Could not save CV questions for session {SessionId}, keeping bank questions", session.Id);
                foreach (var row in rows) _db.Entry(row).State = EntityState.Detached;
                for (int i = 0; i < targets.Count; i++)
                {
                    targets[i].Question = originals[i].Question;
                    targets[i].QuestionId = originals[i].QuestionId;
                    _db.Entry(targets[i]).State = EntityState.Unchanged;
                }
                return 0;
            }
        }
        #endregion

        #region Helpers
        private static string? Clip(string? text, int max) =>
            string.IsNullOrEmpty(text) ? text : text.Length <= max ? text : text[..max];
        #endregion
    }
}
