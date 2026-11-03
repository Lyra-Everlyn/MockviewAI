# Bản Python của bộ chấm SƠ BỘ theo bộ đề (BankKeywordScorer C#) để đo độ chính xác trên câu trả lời mẫu.
# Chạy: python offline_scorer.py <question-bank.v1.json>
import json, sys, unicodedata, re, collections

NEGATORS = {"khong", "tranh", "chang", "cam", "not", "never", "dont", "chua"}
NEG_WINDOW = 3   # số từ đứng trước được xét là phủ định

def normalize(text):
    t = unicodedata.normalize("NFD", (text or "").lower()).replace("đ", "d").replace("Đ", "d")
    t = "".join(ch for ch in t if unicodedata.category(ch) != "Mn")
    t = re.sub(r"[^a-z0-9#+]+", " ", t)
    return t.split()

def find(tokens, phrase):
    """Vị trí bắt đầu các lần xuất hiện cụm từ (theo token)."""
    p = normalize(phrase)
    if not p: return []
    n = len(p)
    return [i for i in range(len(tokens) - n + 1) if tokens[i:i + n] == p]

def hit(tokens, phrase, check_negation=False):
    for i in find(tokens, phrase):
        if check_negation and any(w in NEGATORS for w in tokens[max(0, i - NEG_WINDOW):i]):
            continue
        return True
    return False

def score(q, answer):
    tokens = normalize(answer)
    for m in q["mistakes"]:
        if any(hit(tokens, s, check_negation=True) for s in m.get("signals", [])):
            return "sai", m["id"], []
    met = []
    for c in q["corePoints"]:
        hits = sum(1 for k in set(c["keywords"]) if hit(tokens, k))
        if hits >= c.get("minHits", 1): met.append(c["id"])
    # Có dấu hiệu của một đường vòng (cùng công thức nhưng cách khác) -> ưu tiên đường vòng; không thì lấy đường nhiều dấu hiệu nhất
    best, best_hits = None, 0
    for p in q["paths"]:
        h = sum(1 for s in p["signals"] if hit(tokens, s))
        if h > 0 and p["category"] == "hoi_dac_biet":
            best, best_hits = p, h
            break
        if h > best_hits: best, best_hits = p, h
    path_id = best["id"] if best else "ngoai_bo"
    if len(met) < q["minCorePoints"]:
        return "sai", path_id, met
    if best and best["category"] == "hoi_dac_biet":
        return "hoi_dac_biet", path_id, met
    return "dung", path_id, met

if __name__ == "__main__":
    bank = json.load(open(sys.argv[1], encoding="utf-8"))
    cats = ["dung", "hoi_dac_biet", "dac_biet", "sai"]
    cm = collections.Counter()
    wrong = []
    for q in bank["questions"]:
        for s in q["samples"]:
            got, path, met = score(q, s["answer"])
            cm[(s["category"], got)] += 1
            if not (got == s["category"] or (s["category"] == "dac_biet" and got in ("dung", "hoi_dac_biet"))):
                wrong.append((q["id"], s["category"], got, path, met))
    print("Hàng = nhãn mẫu, cột = chấm sơ bộ")
    print(" " * 14 + "".join(c.rjust(14) for c in cats))
    for a in cats:
        print(a.ljust(14) + "".join(str(cm[(a, b)]).rjust(14) for b in cats))
    total = sum(cm.values())
    correct_like = lambda a: a in ("dung", "hoi_dac_biet", "dac_biet")
    print("\nCâu ĐÚNG bản chất (dung/hoi/dac_biet) bị chấm SAI (chấm oan):",
          sum(v for (a, b), v in cm.items() if correct_like(a) and b == "sai"), "/", sum(v for (a, b), v in cm.items() if correct_like(a)))
    print("Câu SAI bị chấm thành đúng (nguy hiểm):", sum(v for (a, b), v in cm.items() if a == "sai" and b != "sai"), "/", sum(v for (a, b), v in cm.items() if a == "sai"))
    print("Đúng/sai bản chất khớp:", sum(v for (a, b), v in cm.items() if correct_like(a) == (b != "sai")), "/", total)
    if "-v" in sys.argv:
        for w in wrong: print(w)
