"use client";

import { useState } from "react";
import { api } from "@/lib/api";
import { useApi } from "@/lib/hooks";
import type { Page, Review } from "@/lib/types";
import { ReviewCard } from "./ReviewCard";
import { Button, Message, Spinner } from "./ui";

export function ReviewFeed({ path, empty, showUser = true }: { path: string; empty: string; showUser?: boolean }) {
  const first = useApi<Page<Review>>(path);
  const [more, setMore] = useState<{ path: string; page: number; items: Review[] }>({ path: "", page: 1, items: [] });
  const [loading, setLoading] = useState(false);
  const extra = more.path === path ? more : { path, page: 1, items: [] };

  async function loadMore() {
    setLoading(true);
    try {
      const separator = path.includes("?") ? "&" : "?";
      const next = await api<Page<Review>>(`${path}${separator}page=${extra.page + 1}`);
      setMore({ path, page: next.page, items: [...extra.items, ...next.items] });
    } finally {
      setLoading(false);
    }
  }

  if (first.error) return <Message title="Não foi possível carregar">{first.error.message}</Message>;
  if (!first.data) return <Spinner />;
  const items = [...first.data.items, ...extra.items.filter((r) => !first.data!.items.some((f) => f.id === r.id))];
  if (items.length === 0) return <Message title={empty} />;
  return (
    <div className="space-y-3">
      {items.map((review) => (
        <ReviewCard key={review.id} review={review} showTitle showUser={showUser} />
      ))}
      {extra.page < first.data.totalPages ? (
        <div className="flex justify-center pt-2">
          <Button onClick={loadMore} busy={loading}>
            Carregar mais
          </Button>
        </div>
      ) : null}
    </div>
  );
}
