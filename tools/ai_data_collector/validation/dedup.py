"""Cosine similarity tabanlı dedup.

TF-IDF + cosine: sentence-transformers yerine. 1500 örnek için yeter, hızlı.
"""
from __future__ import annotations

from typing import Iterable

import numpy as np
from sklearn.feature_extraction.text import TfidfVectorizer
from sklearn.metrics.pairwise import cosine_similarity


class Dedup:
    def __init__(self, threshold: float = 0.90) -> None:
        self._texts: list[str] = []
        self._threshold = threshold
        self._vec: TfidfVectorizer | None = None
        self._matrix = None  # scipy sparse

    def should_add(self, text: str) -> bool:
        if not self._texts:
            self._texts.append(text)
            self._rebuild()
            return True
        assert self._matrix is not None and self._vec is not None
        q = self._vec.transform([text])
        sims = cosine_similarity(q, self._matrix).ravel()
        if float(np.max(sims)) >= self._threshold:
            return False
        self._texts.append(text)
        self._rebuild()
        return True

    def _rebuild(self) -> None:
        self._vec = TfidfVectorizer(analyzer="char_wb", ngram_range=(3, 5), max_features=5000)
        self._matrix = self._vec.fit_transform(self._texts)

    def __len__(self) -> int:
        return len(self._texts)

    def load_existing(self, texts: Iterable[str]) -> None:
        for t in texts:
            if t:
                self._texts.append(t)
        if self._texts:
            self._rebuild()
