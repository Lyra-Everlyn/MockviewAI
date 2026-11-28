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

            // Giữ câu 1 (giới thiệu). [IT-ROLES] Câu kỹ thuật theo vị trí đặt từ câu 2 trở đi, câu CV thay từ câu cuối ngược lên.
            var slots = FollowUps.Main(session.Answers).Where(a => a.OrderNo > 1).OrderBy(a => a.OrderNo).ToList();
            var cvDrafts = result.Accepted.Where(d => d.Kind == CvQuestionKinds.Cv).Take(slots.Count).ToList();
            var cvSlots = slots.Skip(slots.Count - cvDrafts.Count).ToList();
            var roleDrafts = result.Accepted.Where(d => d.Kind == CvQuestionKinds.Role).Take(slots.Count - cvDrafts.Count).ToList();
            var roleSlots = slots.Take(roleDrafts.Count).ToList();

            var pairs = roleSlots.Zip(roleDrafts).Concat(cvSlots.Zip(cvDrafts)).ToList();
            if (pairs.Count == 0) return 0;

            var targets = pairs.Select(p => p.First).ToList();
            var originals = targets.Select(a => (a.Question, a.QuestionId)).ToList();
            var rows = new List<CvGeneratedQuestion>();
            int cvNo = 0, roleNo = 0;
            for (int i = 0; i < pairs.Count; i++)
            {
                var draft = pairs[i].Second;
                // cv-{sessionId}-{n} cho câu theo CV, ai-{sessionId}-{n} cho câu theo vị trí (cùng bảng)
                string id = draft.Kind == CvQuestionKinds.Role
                    ? $"{QuestionBankStoreExtensions.RoleIdPrefix}{session.Id}-{++roleNo}"
                    : $"{QuestionBankStoreExtensions.CvIdPrefix}{session.Id}-{++cvNo}";
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
                // Không để câu "mồ côi" (có mã cv- / ai- nhưng không có đáp án): trả về câu bộ đề ban đầu
                _logger.LogWarning(ex, "Could not save generated questions for session {SessionId}, keeping bank questions", session.Id);
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
