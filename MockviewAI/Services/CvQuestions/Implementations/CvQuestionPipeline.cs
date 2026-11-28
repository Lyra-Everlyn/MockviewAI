using System.Diagnostics;
using Microsoft.Extensions.Options;
using MockviewAI.Models.CvQuestions;
using MockviewAI.Services.CvQuestions.Interfaces;

namespace MockviewAI.Services.CvQuestions.Implementations
{
    /// <summary>
    /// [CV-QUESTIONS] [IT-ROLES]
    /// Soạn song song (Gemini) câu theo CV (N+1) và câu kỹ thuật theo vị trí (M+1, khi vị trí chưa có bộ đề)
    /// -> luật code -> Groq duyệt MỘT lần cho cả hai loại -> câu "fix" sửa đúng 1 lần -> luật code -> Groq duyệt lại
    /// -> lấy tối đa N câu CV và M câu vị trí. Cả quá trình giới hạn CvQuestions:TimeoutSeconds.
    /// Một loại soạn lỗi thì loại kia vẫn dùng được. Groq lỗi / hết giờ ở vòng đầu: trả rỗng (buổi dùng 100% bộ đề).
    /// Lỗi / hết giờ ở vòng sửa: giữ các câu đã đạt ở vòng đầu.
    /// Log chỉ ghi số câu, model, thời gian, loại lỗi: KHÔNG ghi nội dung CV hay câu hỏi.
    /// </summary>
    public class CvQuestionPipeline : ICvQuestionPipeline
    {
        #region Fields
        private readonly ICvQuestionGenerator _generator;
        private readonly ICvQuestionReviewer _reviewer;
        private readonly ICvQuestionRules _rules;
        private readonly CvQuestionOptions _options;
        private readonly ILogger<CvQuestionPipeline> _logger;
        #endregion

        #region Constructor
        public CvQuestionPipeline(ICvQuestionGenerator generator, ICvQuestionReviewer reviewer, ICvQuestionRules rules,
                                  IOptions<CvQuestionOptions> options, ILogger<CvQuestionPipeline> logger)
        {
            _generator = generator;
            _reviewer = reviewer;
            _rules = rules;
            _options = options.Value;
            _logger = logger;
        }
        #endregion

        #region ICvQuestionPipeline
        public async Task<CvQuestionPipelineResult> RunAsync(CvQuestionContext ctx, CancellationToken ct = default)
        {
            // Câu CV ưu tiên lấy chỗ trước; câu theo vị trí lấy phần chỗ còn lại (tối đa RoleQuestionLimit)
            int cvWanted = string.IsNullOrWhiteSpace(ctx.CvSummary) ? 0 : _options.QuestionLimit;
            var wanted = new Dictionary<string, int>
            {
                [CvQuestionKinds.Cv] = cvWanted,
                [CvQuestionKinds.Role] = ctx.RoleQuestionCount <= 0 ? 0 : Math.Clamp(ctx.RoleQuestionCount - cvWanted, 0, _options.RoleQuestionLimit)
            };
            if (!_options.Enabled || wanted.Values.All(n => n == 0))
                return CvQuestionPipelineResult.Empty;

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutLimit));
            var watch = Stopwatch.StartNew();
            var result = new CvQuestionPipelineResult();
            var stats = new CvQuestionPipelineStats();

            // Mọi model Groq vừa báo hết lượt -> không soạn (chắc chắn không duyệt được), đỡ tốn lượt Gemini
            if (!_reviewer.IsAvailable)
                return Finish(result, stats, watch, "reviewer rate-limited, skipped generation");

            // Vòng 1: soạn (song song từng loại) + duyệt chung. Groq lỗi / hết giờ ở đây -> bỏ hẳn, dùng bộ đề
            List<(CvQuestionDraft Draft, CvQuestionReview Review)> toFix;
            try
            {
                var drafts = await GenerateAllAsync(ctx, wanted.Where(w => w.Value > 0).ToDictionary(w => w.Key, w => w.Value + 1), null, result, timeout.Token);
                stats.Generated = drafts.Count;

                var candidates = ApplyRules(drafts, ctx, result.Accepted, stats);
                if (candidates.Count == 0)
                    return Finish(result, stats, watch, "no candidate passed code rules");

                var reviews = await _reviewer.ReviewAsync(ctx, candidates, timeout.Token);
                result.ReviewerModel = _reviewer.Model;   // model thực sự đã duyệt (chính hoặc dự phòng)
                toFix = new();
                for (int i = 0; i < candidates.Count; i++)
                {
                    var review = reviews[i];
                    if (review.Verdict == CvQuestionReview.Pass)
                    {
                        candidates[i].ReviewVerdict = CvQuestionReview.Pass;
                        result.Accepted.Add(candidates[i]);
                        stats.Passed++;
                    }
                    else if (review.Verdict == CvQuestionReview.Fix) { toFix.Add((candidates[i], review)); stats.FixRequested++; }
                    else stats.Rejected++;
                }
            }
            catch (OperationCanceledException) when (!ct.IsCancellationRequested)
            {
                result.Accepted.Clear();
                return Finish(result, stats, watch, "timeout");
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                result.Accepted.Clear();
                return Finish(result, stats, watch, "error " + ex.GetType().Name);
            }

            // Vòng sửa: đúng 1 lần cho các câu "fix" của loại còn thiếu câu
            var fixable = toFix
                .GroupBy(f => f.Draft.Kind)
                .SelectMany(g => g.Take(Math.Max(0, wanted[g.Key] - result.Accepted.Count(a => a.Kind == g.Key))))
                .ToList();
            if (fixable.Count > 0 && _options.FixRoundLimit > 0)
            {
                try
                {
                    await FixRoundAsync(ctx, fixable, result, stats, timeout.Token);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    _logger.LogWarning("Generated question fix round timed out, keeping {Count} first-round question(s)", result.Accepted.Count);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning("Generated question fix round failed ({Error}), keeping {Count} first-round question(s)", ex.GetType().Name, result.Accepted.Count);
                }
            }

            // Mỗi loại lấy tối đa số câu cần
            result.Accepted = result.Accepted.GroupBy(a => a.Kind).SelectMany(g => g.Take(wanted[g.Key])).ToList();
            return Finish(result, stats, watch, null);
        }
        #endregion

        #region Helpers
        /// <summary>
        /// Soạn song song từng loại. Một loại lỗi (AI hỏng, hết lượt) thì bỏ loại đó, loại kia vẫn dùng.
        /// Hết giờ thì ném OperationCanceledException để vòng ngoài xử lý.
        /// </summary>
        private async Task<List<CvQuestionDraft>> GenerateAllAsync(CvQuestionContext ctx, Dictionary<string, int> counts,
                                                                   Dictionary<string, List<string>>? fixNotes, CvQuestionPipelineResult result, CancellationToken ct)
        {
            var tasks = counts.Select(async c =>
            {
                try
                {
                    var notes = fixNotes != null && fixNotes.TryGetValue(c.Key, out var n) ? n : null;
                    return await _generator.GenerateAsync(ctx, c.Key, c.Value, notes, ct);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning("Generating {Kind} questions failed ({Error}), continuing without them", c.Key, ex.GetType().Name);
                    return null;
                }
            }).ToList();

            var generated = (await Task.WhenAll(tasks)).Where(g => g != null).Select(g => g!).ToList();
            if (generated.Count > 0 && string.IsNullOrEmpty(result.GeneratorModel)) result.GeneratorModel = generated[0].Model;
            return generated.SelectMany(g => g.Drafts).ToList();
        }

        private async Task FixRoundAsync(CvQuestionContext ctx, List<(CvQuestionDraft Draft, CvQuestionReview Review)> toFix,
                                         CvQuestionPipelineResult result, CvQuestionPipelineStats stats, CancellationToken ct)
        {
            var byKind = toFix.GroupBy(f => f.Draft.Kind).ToDictionary(g => g.Key, g => g.ToList());
            var notes = byKind.ToDictionary(
                g => g.Key,
                g => g.Value.Select(f => $"Câu cũ: \"{f.Draft.Question.Question}\". Cần sửa: {string.Join("; ", f.Review.Issues)}").ToList());
            var drafts = await GenerateAllAsync(ctx, byKind.ToDictionary(g => g.Key, g => g.Value.Count), notes, result, ct);
            stats.FixCalls += byKind.Count;

            // Câu sửa trả về theo thứ tự câu cũ (trong từng loại) -> gắn lý do sửa tương ứng để lưu DB; bỏ câu thừa
            var kept = new List<CvQuestionDraft>();
            foreach (var group in drafts.GroupBy(d => d.Kind))
            {
                var originals = byKind[group.Key];
                var list = group.Take(originals.Count).ToList();
                for (int i = 0; i < list.Count; i++) list[i].ReviewNotes = string.Join("; ", originals[i].Review.Issues);
                kept.AddRange(list);
            }

            var candidates = ApplyRules(kept, ctx, result.Accepted, stats);
            if (candidates.Count == 0) return;

            var reviews = await _reviewer.ReviewAsync(ctx, candidates, ct);
            result.ReviewerModel = _reviewer.Model;   // model dự phòng có thể khác vòng đầu
            for (int i = 0; i < candidates.Count; i++)
            {
                if (reviews[i].Verdict == CvQuestionReview.Pass)
                {
                    candidates[i].ReviewVerdict = CvQuestionReview.Fixed;
                    result.Accepted.Add(candidates[i]);
                    stats.Fixed++;
                }
                else stats.Rejected++;
            }
        }

        /// <summary>Luật code; tránh trùng cả câu bộ đề lẫn câu AI soạn đã nhận / đã qua luật trong cùng lượt.</summary>
        private List<CvQuestionDraft> ApplyRules(IEnumerable<CvQuestionDraft> drafts, CvQuestionContext ctx, List<CvQuestionDraft> accepted, CvQuestionPipelineStats stats)
        {
            var passed = new List<CvQuestionDraft>();
            foreach (var draft in drafts)
            {
                var others = accepted.Concat(passed).Select(d => d.Question.Question);
                var problem = _rules.FindProblem(draft, ctx, others);
                if (problem == null) passed.Add(draft);
                else
                {
                    stats.RuleRejected++;
                    _logger.LogInformation("{Kind} question dropped by code rules: {Reason}", draft.Kind, problem);   // lý do là chữ cố định, không chứa CV
                }
            }
            return passed;
        }

        private CvQuestionPipelineResult Finish(CvQuestionPipelineResult result, CvQuestionPipelineStats stats, Stopwatch watch, string? failure)
        {
            if (failure != null)
                _logger.LogWarning("Generated questions not used ({Failure}) after {Ms} ms: generated {Generated}, rule-rejected {RuleRejected}, pass {Pass}, fix {Fix}, reject {Reject}. Using question bank.",
                    failure, watch.ElapsedMilliseconds, stats.Generated, stats.RuleRejected, stats.Passed, stats.FixRequested, stats.Rejected);
            else
                _logger.LogInformation("Generated questions ready in {Ms} ms ({Generator} -> {Reviewer}): generated {Generated}, rule-rejected {RuleRejected}, pass {Pass}, fix {Fix} (fix calls {FixCalls}, fixed {Fixed}), reject {Reject}, used {Cv} cv + {Role} role",
                    watch.ElapsedMilliseconds, result.GeneratorModel, result.ReviewerModel, stats.Generated, stats.RuleRejected, stats.Passed,
                    stats.FixRequested, stats.FixCalls, stats.Fixed, stats.Rejected,
                    result.Accepted.Count(a => a.Kind == CvQuestionKinds.Cv), result.Accepted.Count(a => a.Kind == CvQuestionKinds.Role));
            return result;
        }
        #endregion
    }
}
