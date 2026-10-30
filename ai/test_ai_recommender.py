import unittest
from unittest.mock import patch
from ai_recommender import recommend, run_once


class RecommenderTests(unittest.TestCase):
    def test_recent_similarity_excludes_purchased(self):
        bought = {"id": 1, "categoryId": 1, "brandId": 2, "switchType": "Linear", "sound": "Thocky", "price": 100}
        close = dict(bought, id=2, price=110)
        distant = {"id": 3, "categoryId": 9, "brandId": 9, "price": 100}
        self.assertEqual([2], recommend([bought, distant, close], [bought]))

    def test_random_is_unique_capped_and_uses_available_products(self):
        products = [{"id": i} for i in range(20)]
        result = recommend(products, [])
        self.assertEqual(8, len(set(result)))
        self.assertTrue(set(result) <= set(range(20)))
        self.assertEqual([], recommend([], []))
        self.assertEqual([3], recommend([{"id": 3}], []))

    def test_more_recent_purchase_has_priority(self):
        newest = {"id": 1, "categoryId": 1}
        older = {"id": 2, "categoryId": 2}
        self.assertEqual([4, 3], recommend([
            {"id": 3, "categoryId": 2}, {"id": 4, "categoryId": 1}], [newest, older]))

    @patch("ai_recommender.post")
    def test_disabled_does_not_publish(self, post):
        post.return_value = {"isEnabled": False}
        run_once("http://localhost", "test")
        self.assertEqual(1, post.call_count)

    @patch("ai_recommender.post")
    def test_publishes_personal_and_random_results(self, post):
        post.side_effect = [{"isEnabled": True, "version": "v1", "products": [
            {"id": 2, "categoryId": 1}], "histories": [
            {"userId": 7, "products": [{"id": 1, "categoryId": 1}]}]}, {}]
        run_once("http://localhost", "test")
        self.assertEqual({"version": "v1", "random": [2],
                          "customers": [{"userId": 7, "productIds": [2]}]}, post.call_args.args[3])
