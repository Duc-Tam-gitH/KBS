"""Recommend similar products from recent completed purchases; random for new customers."""
import random
from ai_auto import post, run_worker


def similarity(product, purchased):
    score = 0
    for field, weight in (("categoryId", 4), ("switchType", 3), ("sound", 3), ("layout", 2), ("brandId", 1)):
        left, right = product.get(field), purchased.get(field)
        if left and right and str(left).strip().casefold() == str(right).strip().casefold():
            score += weight
    price, previous_price = float(product.get("price") or 0), float(purchased.get("price") or 0)
    if score and max(price, previous_price) > 0:
        score += 1 - abs(price - previous_price) / max(price, previous_price)
    return score


def recommend(products, purchased):
    bought = {item["id"] for item in purchased}
    candidates = [item for item in products if item["id"] not in bought]
    if not purchased:
        return [item["id"] for item in random.sample(candidates, min(8, len(candidates)))]
    ranked = [(max(similarity(item, old) / (1 + index * 0.1)
                   for index, old in enumerate(purchased)), item["id"]) for item in candidates]
    ranked.sort(key=lambda item: (-item[0], item[1]))
    similar = [pid for score, pid in ranked if score > 0][:8]
    # If no shared attributes exist, offer other available products instead of an empty shelf.
    return similar or [item["id"] for item in random.sample(candidates, min(8, len(candidates)))]


def run_once(base_url, key):
    batch = post(base_url, key, "/api/ai-worker/recommender/poll")
    if not batch["isEnabled"]:
        return
    post(base_url, key, "/api/ai-worker/recommender/results", {
        "version": batch["version"],
        "random": recommend(batch["products"], []),
        "customers": [{"userId": history["userId"],
                       "productIds": recommend(batch["products"], history["products"])}
                      for history in batch["histories"]],
    })


if __name__ == "__main__":
    try:
        run_worker(run_once)
    except KeyboardInterrupt:
        pass
