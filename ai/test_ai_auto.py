import unittest
from unittest.mock import patch
from ai_auto import decision, run_once


class AutoTests(unittest.TestCase):
    def test_boundary(self):
        self.assertEqual("CONFIRM", decision("9999999.99", "10000000"))
        self.assertEqual("MANUAL_REVIEW", decision("10000000", "10000000"))
        self.assertEqual("MANUAL_REVIEW", decision("10000001", "10000000"))

    @patch("ai_auto.post")
    def test_disabled_does_not_decide(self, post):
        post.return_value = {"isEnabled": False}
        run_once("http://localhost", "test")
        self.assertEqual(1, post.call_count)

    @patch("ai_auto.post")
    def test_enabled_sends_decision(self, post):
        post.side_effect = [
            {"isEnabled": True, "approvalLimit": 10000000, "orders": [{"id": 1, "totalPrice": 9999999}]},
            {"result": "CONFIRMED"},
        ]
        run_once("http://localhost", "test")
        self.assertEqual({"decision": "CONFIRM"}, post.call_args.args[3])


if __name__ == "__main__":
    unittest.main()
