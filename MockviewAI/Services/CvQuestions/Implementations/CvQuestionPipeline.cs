using System.Diagnostics;
using Microsoft.Extensions.Options;
using MockviewAI.Models.CvQuestions;
using MockviewAI.Services.CvQuestions.Interfaces;

namespace MockviewAI.Services.CvQuestions.Implementations
{
    /// <summary>
    /// [CV-QUESTIONS]
    /// Soạn N+1 câu (Gemini) -> luật code -> Groq duyệt -> câu "fix" sửa đúng 1 lần -> luật code -> Groq duyệt lại
    /// -> lấy tối đa N câu đạt. Cả quá trình giới hạn CvQuestions:TimeoutSeconds.
    /// Lỗi / hết giờ ở vòng đầu: trả rỗng (buổi dùng 100% bộ đề). Lỗi / hết giờ ở vòng sửa: giữ các câu đã đạt ở vòng đầu.
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
            int wanted = _options.QuestionLimit;
            if (!_options.Enabled || wanted == 0 || string.IsNullOrWhiteSpace(ctx.CvSummary))
                return CvQuestionPipelineResult.Empty;

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
            timeout.CancelAfter(TimeSpan.FromSeconds(_options.TimeoutLimit));
            var watch = Stopwatch.StartNew();
            var result = new CvQuestionPipelineResult();
            var stats = new CvQuestionPipelineStats();

            // Vòng 1: soạn + duyệt. Lỗi ở đây -> bỏ hẳn, dùng bộ đề
            List<(CvQuestionDraft Draft, CvQuestionReview Review)> toFix;
            try
            {
                var generated = await _generator.GenerateAsync(ctx, wanted + 1, null, timeout.Token);
                result.GeneratorModel = generated.Model;
                stats.Generated = generated.Drafts.Count;

                var candidates = ApplyRules(generated.Drafts, ctx, result.Accepted, stats);
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

            // Vòng sửa: đúng 1 lần cho các câu "fix", chỉ khi còn thiếu câu
            if (result.Accepted.Count < wanted && toFix.Count > 0 && _options.FixRoundLimit > 0)
            {
                try
                {
                    await FixRoundAsync(ctx, toFix.Take(wanted - result.Accepted.Count).ToList(), result, stats, timeout.Token);
                }
                catch (OperationCanceledException) when (!ct.IsCancellationRequested)
                {
                    _logger.LogWarning("CV question fix round timed out, keeping {Count} first-round question(s)", result.Accepted.Count);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogWarning("CV question fix round failed ({Error}), keeping {Count} first-round question(s)", ex.GetType().Name, result.Accepted.Count);
                }
            }

            result.Accepted = result.Accepted.Take(wanted).ToList();
            return Finish(result, stats, watch, null);
        }
        #endregion

        #region Helpers
        private async Task FixRoundAsync(CvQuestionContext ctx, List<(CvQuestionDraft Draft, CvQuestionReview Review)> toFix,
                                         CvQuestionPipelineResult result, CvQuestionPipelineStats stats, CancellationToken ct)
        {
            var notes = toFix.Select(f => $"Câu cũ: \"{f.Draft.Question.Question}\". Cần sửa: {string.Join("; ", f.Review.Issues)}").ToList();
            var generated = await _generator.GenerateAsync(ctx, toFix.Count, notes, ct);
            stats.FixCalls++;

            // Câu sửa trả về theo thứ tự câu cũ -> gắn lý do sửa tương ứng để lưu DB
            for (int i = 0; i < generated.Drafts.Count && i < toFix.Count; i++)
                generated.Drafts[i].ReviewNotes = string.Join("; ", toFix[i].Review.Issues);

            var candidates = ApplyRules(generated.Drafts.Take(toFix.Count), ctx, result.Accepted, stats);
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

        /// <summary>Luật code; tránh trùng cả câu bộ đề lẫn câu CV đã nhận / đã qua luật trong cùng lượt.</summary>
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
                    _logger.LogInformation("CV question dropped by code rules: {Reason}", problem);   // lý do là chữ cố định, không chứa CV
                }
            }
            return passed;
        }

        private CvQuestionPipelineResult Finish(CvQuestionPipelineResult result, CvQuestionPipelineStats stats, Stopwatch watch, string? failure)
        {
            if (failure != null)
                _logger.LogWarning("CV questions not used ({Failure}) after {Ms} ms: generated {Generated}, rule-rejected {RuleRejected}, pass {Pass}, fix {Fix}, reject {Reject}. Using question bank.",
                    failure, watch.ElapsedMilliseconds, stats.Generated, stats.RuleRejected, stats.Passed, stats.FixRequested, stats.Rejected);
            else
                _logger.LogInformation("CV questions ready in {Ms} ms ({Generator} -> {Reviewer}): generated {Generated}, rule-rejected {RuleRejected}, pass {Pass}, fix {Fix} (fix calls {FixCalls}, fixed {Fixed}), reject {Reject}, used {Used}",
                    watch.ElapsedMilliseconds, result.GeneratorModel, result.ReviewerModel, stats.Generated, stats.RuleRejected, stats.Passed,
                    stats.FixRequested, stats.FixCalls, stats.Fixed, stats.Rejected, result.Accepted.Count);
            return result;
        }
        #endregion
    }
}
