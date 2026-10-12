using System;

namespace MockviewAI.ViewModels
{
    public class ProfileEditViewModel
    {
        public string FullName { get; set; }
        public string Major { get; set; }
        public string Position { get; set; }
        public string StudentStatus { get; set; }
    }

    public class SessionResultViewModel
    {
        public string SessionId { get; set; }

        public int ContentScore { get; set; }
        public int DeliveryScore { get; set; }

        public int TotalScore => (int)Math.Round(ContentScore * 0.6 + DeliveryScore * 0.4);

        public int AnxietyBefore { get; set; }
        public int AnxietyAfter { get; set; }
    }
}
