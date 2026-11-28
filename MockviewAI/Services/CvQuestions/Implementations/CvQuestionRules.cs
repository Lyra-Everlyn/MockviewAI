using MockviewAI.Models.CvQuestions;
using MockviewAI.Services.CvQuestions.Interfaces;
using MockviewAI.Services.InterviewQuestions.Implementations;

namespace MockviewAI.Services.CvQuestions.Implementations
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Luật code cho câu AI soạn (mục 7a spec V3). So khớp chữ sau khi chuẩn hoá bằng VietnameseTextNormalizer
    /// (bỏ dấu, chữ thường) nên "Sức khỏe" và "suc khoe" như nhau.
    /// </summary>
    public class CvQuestionRules : ICvQuestionRules
    {
        #region Fields
        public const int MaxQuestionChars = 250;
        private const int MinCorePoints = 2, MaxCorePoints = 4;
        private const int MinKeywords = 2;
        private const int MinPaths = 1, MaxPaths = 2;

        /// <summary>Tỉ lệ từ trùng (Jaccard) từ mức này trở lên coi là gần trùng câu khác.</summary>
        private const double DuplicateThreshold = 0.7;

        /// <summary>Tỉ lệ từ của cvEvidence phải có trong tóm tắt CV (cho phép khớp một phần).</summary>
        private const double EvidenceThreshold = 0.6;

        private static readonly HashSet<string> PathCategories = new() { "dung", "hoi_dac_biet" };

        /// <summary>
        /// Chủ đề riêng tư / dễ phân biệt đối xử, viết sẵn dạng đã chuẩn hoá (không dấu).
        /// Không dùng từ dễ nhầm với tên dự án / thuật ngữ: "luong" (luồng / thread), "benh" (quản lý bệnh viện),
        /// "gia dinh" / "thu nhap" (app chi tiêu gia đình), "nam sinh" ("năm sinh viên").
        /// </summary>
        private static readonly string[] PrivatePhrases =
        {
            "bao nhieu tuoi", "tuoi tac", "gia dinh ban", "hoan canh gia dinh", "bo me ban", "ket hon", "hon nhan",
            "nguoi yeu", "con cai", "co con", "sinh con", "mang thai", "ton giao", "dao phat", "thien chua",
            "suc khoe", "benh ly", "tien su benh", "muc luong", "luong mong muon", "luong bao nhieu",
            "thu nhap cua ban", "thu nhap mong muon", "dan toc", "chinh tri", "dang phai", "gioi tinh",
            "xu huong tinh duc", "que quan", "que o dau", "ngoai hinh", "can nang cua ban", "chieu cao cua ban"
        };
        #endregion

        #region ICvQuestionRules
        public string? FindProblem(CvQuestionDraft draft, CvQuestionContext ctx, IEnumerable<string> otherQuestions)
        {
            var q = draft.Question;
            string text = (q.Question ?? string.Empty).Trim();

            if (text.Length == 0) return "Câu hỏi rỗng";
            if (text.Length > MaxQuestionChars) return $"Câu hỏi dài quá {MaxQuestionChars} ký tự";

            var tokens = VietnameseTextNormalizer.Tokenize(text);
            foreach (var other in ctx.ExistingQuestions.Concat(otherQuestions))
            {
                if (Similarity(tokens, VietnameseTextNormalizer.Tokenize(other)) >= DuplicateThreshold)
                    return "Trùng hoặc gần trùng câu đã có trong buổi";
            }

            if (q.CorePoints.Count < MinCorePoints || q.CorePoints.Count > MaxCorePoints)
                return $"Số ý cốt lõi phải từ {MinCorePoints} đến {MaxCorePoints}";
            if (q.CorePoints.Any(c => string.IsNullOrWhiteSpace(c.Text)))
                return "Có ý cốt lõi rỗng";
            if (q.CorePoints.Any(c => c.Keywords.Count(k => !string.IsNullOrWhiteSpace(k)) < Math.Max(MinKeywords, c.MinHits)))
                return "Có ý cốt lõi thiếu từ khoá";
            if (q.CorePoints.Select(c => c.Id).Distinct().Count() != q.CorePoints.Count)
                return "Mã ý cốt lõi bị trùng";
            if (q.MinCorePoints < 1 || q.MinCorePoints > q.CorePoints.Count)
                return "minCorePoints không hợp lệ (lớn hơn số ý)";

            if (q.Paths.Count < MinPaths || q.Paths.Count > MaxPaths)
                return $"Số con đường đúng phải từ {MinPaths} đến {MaxPaths}";
            if (q.Paths.Any(p => !PathCategories.Contains(p.Category)))
                return "Loại con đường không hợp lệ";
            if (!q.Paths.Any(p => p.Category == "dung"))
                return "Thiếu con đường chuẩn (dung)";
            var coreIds = q.CorePoints.Select(c => c.Id).ToHashSet();
            if (q.Paths.Any(p => p.CoversCorePoints.Count == 0 || p.CoversCorePoints.Any(id => !coreIds.Contains(id))))
                return "coversCorePoints trỏ tới ý không tồn tại";

            string normalized = " " + string.Join(' ', tokens) + " ";
            if (PrivatePhrases.Any(p => normalized.Contains(" " + p + " ")))
                return "Câu hỏi chạm chủ đề riêng tư";

            // [IT-ROLES] Câu kỹ thuật theo vị trí là kiến thức chuẩn của nghề, không dựa vào CV -> không kiểm căn cứ CV
            if (draft.Kind != CvQuestionKinds.Role && !EvidenceInCv(draft.CvEvidence, ctx.CvSummary))
                return "cvEvidence rỗng hoặc không có trong CV";

            return null;
        }
        #endregion

        #region Helpers
        /// <summary>Jaccard theo tập từ: |chung| / |hợp|.</summary>
        private static double Similarity(string[] a, string[] b)
        {
            if (a.Length == 0 || b.Length == 0) return 0;
            var setA = a.ToHashSet();
            var setB = b.ToHashSet();
            int common = setA.Count(setB.Contains);
            return common / (double)(setA.Count + setB.Count - common);
        }

        /// <summary>Đủ EvidenceThreshold số từ (khác nhau) của đoạn trích có mặt trong tóm tắt CV.</summary>
        private static bool EvidenceInCv(string? evidence, string cvSummary)
        {
            var words = VietnameseTextNormalizer.Tokenize(evidence).Distinct().ToList();
            if (words.Count == 0) return false;
            var cvWords = VietnameseTextNormalizer.Tokenize(cvSummary).ToHashSet();
            int found = words.Count(cvWords.Contains);
            return found / (double)words.Count >= EvidenceThreshold;
        }
        #endregion
    }
}
