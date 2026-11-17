import re
import unicodedata

from urllib.error import HTTPError
from ai_auto import post, run_worker


# Bộ từ điển NLP rác (Fake reviews/Spam)
# Chỉ dùng cụm từ có ý nghĩa spam, tránh từ đơn như "mua", "tiền", "link"
# để không đánh dấu nhầm các đánh giá thật.
SPAM_KEYWORDS = {
    "kiếm tiền online",
    "việc nhẹ lương cao",
    "thu nhập khủng",
    "nhận thưởng ngay",
    "trúng thưởng",
    "vay tiền nhanh",
    "vay không thế chấp",
    "nhà cái",
    "cá cược",
    "đánh bạc",
    "nổ hũ",
    "tăng follow",
    "buff like",
    "buff mắt",
}


def normalize(text):
    text = unicodedata.normalize("NFD", text.casefold())
    text = "".join(c for c in text if not unicodedata.combining(c))
    text = text.replace("đ", "d")
    text = re.sub(r"[\W_]+", " ", text)
    return " ".join(text.split())


NORMALIZED_KEYWORDS = {
    normalize(keyword) for keyword in SPAM_KEYWORDS
}


def find_spam_keywords(content):
    # Lưu ý: cách này chưa phát hiện được các kiểu viết né như "k.i.ế.m t.i.ề.n"
    # (cần bộ lọc nâng cao hơn nếu muốn chặn triệt để).
    content = f" {normalize(content)} "
    return [
        keyword
        for keyword in sorted(NORMALIZED_KEYWORDS)
        if f" {keyword} " in content
    ]


def run_once(base_url, key):
    batch = post(base_url, key, "/api/ai-worker/reviewnpl/poll")
    if not batch["isEnabled"]:
        return

    for review in batch["reviews"]:
        matched = find_spam_keywords(review["comment"])

        try:
            post(
                base_url,
                key,
                f'/api/ai-worker/reviewnpl/reviews/{review["id"]}/decision',
                {
                    "decision": "FLAGGED" if matched else "APPROVED",
                    "matchedKeywords": matched,
                },
            )
        except HTTPError as error:
            if error.code not in (404, 409):
                raise


if __name__ == "__main__":
    try:
        run_worker(run_once)
    except KeyboardInterrupt:
        pass