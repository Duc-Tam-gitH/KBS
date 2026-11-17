import unittest
from unittest.mock import patch
from ai_reviewnpl import find_spam_keywords, run_once


class ReviewTests(unittest.TestCase):
    def test_normalization_and_legitimate_review(self):
        for comment in ('VIỆC   NHẸ LƯƠNG CAO!', 'kiem tien online', 'Nhận-thưởng-ngay'):
            self.assertTrue(find_spam_keywords(comment))
        self.assertFalse(find_spam_keywords('Bàn phím đáng tiền, shop giao nhanh'))

    @patch('ai_reviewnpl.post')
    def test_disabled(self, post):
        post.return_value = {'isEnabled': False}
        run_once('http://localhost', 'test')
        self.assertEqual(1, post.call_count)

    @patch('ai_reviewnpl.post')
    def test_decisions(self, post):
        post.side_effect = [{'isEnabled': True, 'reviews': [
            {'id': 1, 'comment': 'kiếm tiền online'}, {'id': 2, 'comment': 'Gõ rất tốt'}]}, {}, {}]
        run_once('http://localhost', 'test')
        self.assertEqual('FLAGGED', post.call_args_list[1].args[3]['decision'])
        self.assertEqual('APPROVED', post.call_args_list[2].args[3]['decision'])
