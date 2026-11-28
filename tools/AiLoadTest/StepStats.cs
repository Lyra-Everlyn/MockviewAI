using System.Collections.Concurrent;
using System.Text;

namespace AiLoadTest
{
    /// <summary>Ghi kết quả từng bước (AI chạy được hay phải dùng đường dự phòng) và thời gian, an toàn khi nhiều buổi chạy song song.</summary>
    public class StepStats
    {
        private readonly ConcurrentDictionary<string, ConcurrentBag<(bool FromAi, long Ms)>> _steps = new();
        private readonly List<string> _order = new();

        public void Add(string step, bool fromAi, long ms)
        {
            lock (_order) if (!_order.Contains(step)) _order.Add(step);
            _steps.GetOrAdd(step, _ => new()).Add((fromAi, ms));
        }

        public string Report()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"{"Bước",-34} {"Tổng",5} {"AI",5} {"Dự phòng",9} {"% AI",6} {"p50 ms",8} {"p95 ms",8} {"max ms",8}");
            foreach (var step in _order)
            {
                var list = _steps[step].ToList();
                var ms = list.Select(x => x.Ms).OrderBy(x => x).ToList();
                int ai = list.Count(x => x.FromAi);
                sb.AppendLine($"{step,-34} {list.Count,5} {ai,5} {list.Count - ai,9} {100.0 * ai / list.Count,5:0}% {Pct(ms, 0.5),8} {Pct(ms, 0.95),8} {ms[^1],8}");
            }
            return sb.ToString();
        }

        private static long Pct(List<long> sorted, double p) => sorted[Math.Clamp((int)Math.Ceiling(p * sorted.Count) - 1, 0, sorted.Count - 1)];
    }
}
