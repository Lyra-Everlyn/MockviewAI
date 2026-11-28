using System.Globalization;

namespace AiLoadTest
{
    /// <summary>Tham số dòng lệnh. Mặc định nhỏ (5 buổi) để lỡ chạy cũng không tốn nhiều lượt.</summary>
    public class LoadTestOptions
    {
        public int Sessions { get; private set; } = 5;
        public double SpreadMinutes { get; private set; } = 2;
        public int Answers { get; private set; } = 5;
        public double AnswerGapSeconds { get; private set; } = 20;
        public string CvFile { get; private set; } = Path.Combine(AppContext.BaseDirectory, "sample-cvs.json");
        public string? AudioFile { get; private set; }
        public bool Scoring { get; private set; } = true;
        public bool Confirmed { get; private set; }
        public bool Verbose { get; private set; }

        public static LoadTestOptions Parse(string[] args)
        {
            var o = new LoadTestOptions();
            for (int i = 0; i < args.Length; i++)
            {
                string Next() => i + 1 < args.Length ? args[++i] : throw new ArgumentException($"Thiếu giá trị sau {args[i]}");
                switch (args[i])
                {
                    case "--sessions": o.Sessions = Math.Clamp(int.Parse(Next()), 1, 200); break;
                    case "--spread-minutes": o.SpreadMinutes = Math.Clamp(double.Parse(Next(), CultureInfo.InvariantCulture), 0, 120); break;
                    case "--answers": o.Answers = Math.Clamp(int.Parse(Next()), 1, 10); break;
                    case "--answer-gap": o.AnswerGapSeconds = Math.Clamp(double.Parse(Next(), CultureInfo.InvariantCulture), 0, 600); break;
                    case "--cv-file": o.CvFile = Next(); break;
                    case "--audio": o.AudioFile = Next(); break;
                    case "--no-scoring": o.Scoring = false; break;
                    case "--yes": o.Confirmed = true; break;
                    case "--verbose": o.Verbose = true; break;
                    default: throw new ArgumentException($"Không hiểu tham số {args[i]}");
                }
            }
            return o;
        }
    }
}
