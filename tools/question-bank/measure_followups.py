# Đợt 8: đo tần suất HỎI THÊM trên câu trả lời mẫu (cùng luật với FollowUpPlanner C#).
# Luật: chấm sơ bộ ra "sai" -> hỏi thêm ý cốt lõi đầu tiên chưa đạt (có câu hỏi thêm soạn sẵn).
# Chạy: python measure_followups.py <question-bank.v1.json>
# Đọc kết quả: câu mẫu "sai" nên được hỏi thêm (càng cao càng tốt); câu mẫu đúng / hơi đặc biệt / đặc biệt
# KHÔNG nên bị hỏi thêm (càng thấp càng tốt). "mới" = 4 câu mẫu sau (không dùng để chỉnh từ khoá) -> số khách quan hơn.
import json, sys, os, collections
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from offline_scorer import score, normalize, hit

def met_points(q, answer):
    t = normalize(answer)
    return [c["id"] for c in q["corePoints"] if sum(1 for k in set(c["keywords"]) if hit(t, k)) >= c.get("minHits", 1)]

def plan(q, answer):
    category = score(q, answer)[0]
    if category != "sai": return None
    met = met_points(q, answer)
    return next((c["id"] for c in q["corePoints"] if c["id"] not in met and c.get("followUp")), None)

bank = json.load(open(sys.argv[1], encoding="utf-8-sig"))
stat = collections.defaultdict(lambda: [0, 0])
asked_point = collections.Counter()
for q in bank["questions"]:
    for i, s in enumerate(q["samples"]):
        key = (s["category"], "mới" if i >= 4 else "gốc")
        p = plan(q, s["answer"])
        stat[key][0] += p is not None
        stat[key][1] += 1
        if p: asked_point[p] += 1
print("Loại câu mẫu (nhóm)        | bị hỏi thêm")
for k in sorted(stat):
    print(f"{k[0]:<14} ({k[1]:<3})        | {stat[k][0]}/{stat[k][1]}")
good_new = [stat[(c, "mới")] for c in ("dung", "hoi_dac_biet", "dac_biet")]
print(f"Câu MỚI: hỏi thêm câu sai {stat[('sai', 'mới')][0]}/{stat[('sai', 'mới')][1]}; "
      f"hỏi thừa câu không sai {sum(a for a, _ in good_new)}/{sum(b for _, b in good_new)}")
print("Ý được hỏi thêm:", dict(asked_point))
