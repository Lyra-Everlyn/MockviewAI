# Kiểm tra bộ đề: cấu trúc, mã duy nhất, câu mẫu đủ 4 loại và trỏ đúng con đường, thẻ kỹ năng có trong từ điển.
# Chạy: python validate_bank.py <question-bank.v1.json> <skill-dictionary.v1.json>   (cần Python 3)
import json, sys, collections
bank = json.load(open(sys.argv[1], encoding="utf-8"))
dic = json.load(open(sys.argv[2], encoding="utf-8")) if len(sys.argv) > 2 else {"skills": []}   # từ điển kỹ năng: không bắt buộc (CV đã gỡ)
errs, warns = [], []
tags = {s["tag"] for s in dic["skills"]}
check_tags = len(dic["skills"]) > 0
cats = [c["id"] for c in bank["categories"]]
ids = [q["id"] for q in bank["questions"]]
for i, n in collections.Counter(ids).items():
    if n > 1: errs.append(f"trùng mã câu {i}")
for q in bank["questions"]:
    qid = q["id"]
    core = {c["id"] for c in q["corePoints"]}
    paths = {p["id"]: p for p in q["paths"]}
    mist = {m["id"] for m in q["mistakes"]}
    if len(core) < q["minCorePoints"]: errs.append(f"{qid}: ít ý cốt lõi hơn minCorePoints")
    if not any(p["category"] == "dung" for p in q["paths"]): errs.append(f"{qid}: thiếu đường chuẩn")
    for p in q["paths"]:
        if p["category"] not in ("dung", "hoi_dac_biet"): errs.append(f"{qid}/{p['id']}: loại đường không hợp lệ")
        bad = set(p["coversCorePoints"]) - core
        if bad: errs.append(f"{qid}/{p['id']}: trỏ tới ý không tồn tại {bad}")
        if len(p["coversCorePoints"]) < q["minCorePoints"]: errs.append(f"{qid}/{p['id']}: bao phủ ít hơn minCorePoints")
    if set(paths) & mist: errs.append(f"{qid}: mã đường trùng mã lỗi")
    got = collections.Counter(s["category"] for s in q["samples"])
    for c in cats:
        if got[c] != 2: errs.append(f"{qid}: cần đúng 2 câu mẫu loại {c}, có {got[c]}")
    for s in q["samples"]:
        c, e = s["category"], s["expectedPath"]
        # hoi_dac_biet: đường vòng đã biết HOẶC đường chuẩn nhưng giải thích khác thường
        ok = (c == "dung" and e in paths and paths[e]["category"] == c) or (c == "hoi_dac_biet" and e in paths) \
             or (c == "dac_biet" and e == "ngoai_bo") or (c == "sai" and (e in mist or e == "ngoai_bo"))
        if not ok: errs.append(f"{qid}: câu mẫu {c} trỏ sai đường '{e}'")
        if len(s["answer"]) < 20: warns.append(f"{qid}: câu mẫu {c} quá ngắn")
        if not s.get("style"): errs.append(f"{qid}: câu mẫu {c} thiếu phong cách (style)")
    if q.get("interviewer") not in ("linh", "nam", "an"): errs.append(f"{qid}: interviewer phải là linh | nam | an")
    for t in q["skills"]:
        if check_tags and t not in tags: errs.append(f"{qid}: thẻ '{t}' không có trong từ điển")
    for pos in q["positions"]:
        if pos not in ("developer", "tester", "ba"): errs.append(f"{qid}: vị trí lạ {pos}")
per_pos = collections.Counter(f'{q["interviewer"]}/{p}' for q in bank["questions"] for p in q["positions"])
for key, n in per_pos.items():
    if n < 10: errs.append(f"{key} chỉ có {n} câu (< 10, không đủ buổi 10 câu)")
    iv, pos = key.split("/")
    if not any(q["interviewer"] == iv and pos in q["positions"] and q["id"].endswith("-gen-01") for q in bank["questions"]):
        errs.append(f"{key}: thiếu câu giới thiệu (*-gen-01)")
for s in dic["skills"]:
    used = [q["id"] for q in bank["questions"] if s["tag"] in q["skills"]]
    if not used: errs.append(f"thẻ {s['tag']} chưa có câu hỏi nào")
    for pos in s["positions"]:
        if not any(pos in q["positions"] and s["tag"] in q["skills"] for q in bank["questions"]):
            warns.append(f"thẻ {s['tag']} khai báo cho {pos} nhưng {pos} chưa có câu nào gắn thẻ này")
alias_owner = collections.defaultdict(list)
for s in dic["skills"]:
    for a in s["aliases"]: alias_owner[a.lower()].append(s["tag"])
for a, o in alias_owner.items():
    if len(o) > 1: warns.append(f"alias '{a}' thuộc nhiều thẻ {o}")
print("Số câu:", len(ids), "| theo vị trí:", dict(per_pos), "| câu mẫu:", sum(len(q["samples"]) for q in bank["questions"]))
print("LỖI:", len(errs)); [print("  -", e) for e in errs]
print("CẢNH BÁO:", len(warns)); [print("  -", w) for w in warns]
sys.exit(1 if errs else 0)
