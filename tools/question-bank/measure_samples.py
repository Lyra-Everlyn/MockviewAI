# Đo chấm sơ bộ theo người phỏng vấn, tách câu mẫu GỐC (đã dùng chỉnh từ khoá) và MỚI (chưa chỉnh).
# Chạy: python measure_samples.py <bank.json> <linh|nam|an|all> <goc|moi> [-v]
import json, sys, collections
from offline_scorer import score
TUNING = {"đầy đủ theo công thức chuẩn", "đường vòng đã biết", "cách tiếp cận riêng", "mắc lỗi hay gặp"}
bank = json.load(open(sys.argv[1], encoding="utf-8")); iv = sys.argv[2]; part = sys.argv[3]
cats = ["dung", "hoi_dac_biet", "dac_biet", "sai"]
cm = collections.Counter(); bad = []
for q in bank["questions"]:
    if iv != "all" and q["interviewer"] != iv: continue
    for s in q["samples"]:
        if (s["style"] in TUNING) != (part == "goc"): continue
        got, path, met = score(q, s["answer"])
        cm[(s["category"], got)] += 1
        if (s["category"] != "sai") == (got == "sai") or (s["category"] in ("dung", "hoi_dac_biet") and got != s["category"]):
            bad.append((q["id"], s["category"], got, path, met))
print(f"== {iv} / {part}")
print(" " * 14 + "".join(c.rjust(14) for c in cats))
for a in cats: print(a.ljust(14) + "".join(str(cm[(a, b)]).rjust(14) for b in cats))
n_ok = sum(v for (a, b), v in cm.items() if a != "sai"); n_w = sum(v for (a, b), v in cm.items() if a == "sai")
print("Câu đúng bị chấm oan là Sai:", sum(v for (a, b), v in cm.items() if a != "sai" and b == "sai"), "/", n_ok)
print("Câu Sai lọt thành đúng:", sum(v for (a, b), v in cm.items() if a == "sai" and b != "sai"), "/", n_w)
if "-v" in sys.argv:
    for b in bad: print("  ", b)
