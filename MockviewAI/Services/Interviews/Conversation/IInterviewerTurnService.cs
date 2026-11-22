namespace MockviewAI.Services.Interviews.Conversation
{
    /// <summary>
    /// [INTERVIEWER] Soạn phản hồi của nhà tuyển dụng sau mỗi câu trả lời. Không bao giờ ném lỗi: AI lỗi thì trả phản hồi có sẵn.
    /// </summary>
    public interface IInterviewerTurnService
    {
        Task<InterviewerTurn> NextTurnAsync(InterviewerTurnRequest request, CancellationToken ct = default);
    }
}
