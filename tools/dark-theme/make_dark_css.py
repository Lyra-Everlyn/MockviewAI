# Sinh CSS "bản tối" cho các trang giao diện sáng cũ (Đợt 5, 02/11/2026).
# Ý tưởng: KHÔNG sửa CSS cũ. Đọc từng rule có màu (nền, chữ, viền, bóng, gradient), đổi màu sáng -> màu tối
# theo bảng màu của studio.css, rồi ghi ra file *.dark.css nạp SAU CSS cũ. Mọi khai báo dùng !important và
# selector có thêm "html " phía trước để thắng cả các khai báo !important trong theme.css.
# Chạy: python make_dark_css.py <thư mục wwwroot/css> <thư mục ra wwwroot/css/dark>
import sys, os, re, colorsys
import tinycss2

INK, PANEL, RAISED, LINE = "#0f1626", "#161f34", "#1e2944", "#2a3756"
TEXT, TEXT2, MUTED = "#e8ecf6", "#c3cbe0", "#93a0ba"

NAMED = {"white": (255, 255, 255, 1), "black": (0, 0, 0, 1), "transparent": None}
COLOR_RE = re.compile(r"#[0-9a-fA-F]{3,8}\b|rgba?\([^)]*\)|\bwhite\b|\bblack\b", re.I)

def parse(c):
    c = c.strip().lower()
    if c in NAMED: return NAMED[c]
    if c.startswith("#"):
        h = c[1:]
        if len(h) in (3, 4): h = "".join(ch * 2 for ch in h)
        r, g, b = int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16)
        a = int(h[6:8], 16) / 255 if len(h) == 8 else 1
        return (r, g, b, a)
    nums = re.findall(r"[\d.]+%?", c)
    vals = [float(n[:-1]) * 2.55 if n.endswith("%") else float(n) for n in nums[:3]]
    a = float(nums[3].rstrip("%")) / (100 if nums[3].endswith("%") else 1) if len(nums) > 3 else 1
    return (vals[0], vals[1], vals[2], a)

def hls(rgb):
    return colorsys.rgb_to_hls(rgb[0] / 255, rgb[1] / 255, rgb[2] / 255)

def tint(rgb, light, sat=None, alpha=1):
    h, l, s = hls(rgb)
    r, g, b = colorsys.hls_to_rgb(h, light, s if sat is None else sat)
    r, g, b = round(r * 255), round(g * 255), round(b * 255)
    return f"rgba({r}, {g}, {b}, {alpha})" if alpha < 1 else f"#{r:02x}{g:02x}{b:02x}"

def map_color(tok, role):
    """role: bg | text | border | shadow | body"""
    c = parse(tok)
    if c is None: return tok
    r, g, b, a = c
    h, l, s = hls(c)
    chroma = (max(r, g, b) - min(r, g, b)) / 255
    colorful = chroma > 0.06 and 0.12 < l < 0.985   # có màu rõ (không phải xám pha chút xanh)
    if role == "shadow":
        return f"rgba(0, 0, 0, {min(0.45, max(0.18, a * 1.6)):.2f})"
    if a < 0.5:   # màu mờ đè lên nền: đảo sáng <-> tối, giữ độ mờ
        if colorful: return tint(c, 0.65, min(1, s), round(min(0.22, a + 0.06), 2))
        return f"rgba(232, 236, 246, {round(min(0.12, a + 0.02), 2)})" if l < 0.5 else f"rgba(15, 22, 38, {round(min(0.85, a + 0.5), 2)})"
    if role in ("bg", "body"):
        if colorful and l > 0.82: return tint(c, 0.62, min(0.85, s), 0.14)      # nền nhạt có màu -> nền màu mờ
        if colorful: return tok                                                   # nền màu đậm (nút, hero) giữ nguyên
        if l > 0.9: return INK if role == "body" else PANEL
        if l > 0.75: return RAISED
        if l > 0.45: return LINE
        return tok
    if role == "border":
        if colorful and l > 0.75: return tint(c, 0.62, min(0.85, s), 0.35)
        if colorful: return tint(c, max(l, 0.62))
        return LINE if l > 0.55 else tok
    # text
    if colorful:
        return tint(c, max(l, 0.72)) if l < 0.72 else tok                         # chữ màu đậm -> sáng hơn để đọc trên nền tối
    if l < 0.3: return TEXT
    if l < 0.62: return MUTED
    return tok                                                                    # chữ trắng / sáng giữ nguyên

PROPS = {
    "background": "bg", "background-color": "bg", "background-image": "bg",
    "color": "text", "fill": "text", "caret-color": "text",
    "border": "border", "border-color": "border", "border-top": "border", "border-bottom": "border",
    "border-left": "border", "border-right": "border", "border-top-color": "border", "border-bottom-color": "border",
    "border-left-color": "border", "border-right-color": "border", "outline": "border", "outline-color": "border",
    "box-shadow": "shadow", "text-shadow": "shadow", "stroke": "text",
}

def convert_value(prop, value, selector):
    role = PROPS[prop]
    if role == "bg" and re.search(r"(^|[\s,])(html|body)($|[\s,:.])", selector): role = "body"
    if "var(" in value and not COLOR_RE.search(value): return None
    if role == "shadow" and value.strip() == "none": return None
    if role == "text" and value.strip().lower() in ("inherit", "currentcolor", "transparent"): return None
    new = COLOR_RE.sub(lambda m: map_color(m.group(0), role), value)
    return new if new != value else None

def var_role(name):
    n = name.lower()
    if "shadow" in n: return "shadow"
    if "border" in n or "line" in n: return "border"
    if any(k in n for k in ("text", "muted", "title", "heading", "ink", "dark")): return "text"
    if n in ("--bg-color", "--mv-bg", "--bg", "--background", "--page-bg", "--bg-main", "--background-color"): return "body"
    if any(k in n for k in ("bg", "background", "surface", "card", "glass", "white", "light", "soft")): return "bg"
    return None

def convert_root(decls):
    out = []
    for d in decls:
        if d.type != "declaration" or not d.name.startswith("--"): continue
        value = tinycss2.serialize(d.value).strip()
        if not COLOR_RE.search(value): continue
        role = var_role(d.name)
        if role is None:   # tên không rõ: màu nhạt coi như nền, còn lại giữ (màu thương hiệu)
            c = parse(COLOR_RE.search(value).group(0))
            if c is None or hls(c)[1] <= 0.8: continue
            role = "bg"
        new = COLOR_RE.sub(lambda m: map_color(m.group(0), role), value)
        if new != value: out.append(f"{d.name}: {new};")
    return out

def prefix(sel):
    out = []
    for s in sel.split(","):
        s = s.strip()
        if not s: continue
        if s == ":root": return None
        if s.startswith("html"): out.append("html:root" + s[4:])
        else: out.append("html " + s)
    return ", ".join(out)

def walk(rules, out, indent=""):
    for r in rules:
        if r.type == "qualified-rule":
            sel = tinycss2.serialize(r.prelude).strip()
            if "@" in sel: continue
            decls = tinycss2.parse_declaration_list(r.content, skip_whitespace=True, skip_comments=True)
            if sel == ":root":   # biến màu: đổi theo vai trò đoán từ tên biến
                vs = convert_root(decls)
                if vs: out.append(f"{indent}html:root {{ {' '.join(vs)} }}")
                continue
            new = []
            for d in decls:
                if d.type != "declaration": continue
                p = d.lower_name
                if p not in PROPS: continue
                v = convert_value(p, tinycss2.serialize(d.value).strip(), sel)
                if v: new.append(f"{p}: {v} !important;")
            ps = prefix(sel)
            if new and ps: out.append(f"{indent}{ps} {{ {' '.join(new)} }}")
        elif r.type == "at-rule" and r.lower_at_keyword in ("media", "supports") and r.content:
            inner = []
            walk(tinycss2.parse_rule_list(r.content, skip_whitespace=True, skip_comments=True), inner, indent + "    ")
            if inner:
                out.append(f"{indent}@{r.lower_at_keyword} {tinycss2.serialize(r.prelude).strip()} {{")
                out.extend(inner); out.append(f"{indent}}}")

def convert_file(src, dst):
    rules = tinycss2.parse_stylesheet(open(src, encoding="utf-8-sig").read(), skip_whitespace=True, skip_comments=True)
    out = []
    walk(rules, out)
    head = (f"/* {os.path.basename(dst)} — SINH TỰ ĐỘNG từ {os.path.basename(src)} bởi tools/dark-theme/make_dark_css.py (Đợt 5).\n"
            f"   Không sửa tay: sửa CSS gốc hoặc công cụ rồi chạy lại. Nạp SAU {os.path.basename(src)} và theme.css. */\n")
    open(dst, "w", encoding="utf-8").write(head + "\n".join(out) + "\n")
    return len(out)

if __name__ == "__main__":
    src_dir, dst_dir = sys.argv[1], sys.argv[2]
    os.makedirs(dst_dir, exist_ok=True)
    for name in sys.argv[3:]:
        n = convert_file(os.path.join(src_dir, name), os.path.join(dst_dir, name.replace(".css", ".dark.css")))
        print(f"{name}: {n} rule")
