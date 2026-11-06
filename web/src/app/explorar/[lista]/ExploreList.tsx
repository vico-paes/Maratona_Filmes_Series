"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useState } from "react";
import { ChevronLeft } from "lucide-react";
import { PosterCard, PosterGrid } from "@/components/PosterCard";
import { Button, cx, Message, Spinner } from "@/components/ui";
import { api } from "@/lib/api";
import { useApi } from "@/lib/hooks";
import { findHomeList, homeLists } from "@/lib/lists";
import type { Page, TitleCard } from "@/lib/types";

function ListContent({ api: apiKey }: { api: string }) {
  const first = useApi<Page<TitleCard>>(`/home/${apiKey}`);
  const [more, setMore] = useState<{ page: number; items: TitleCard[] }>({ page: 1, items: [] });
  const [loading, setLoading] = useState(false);

  async function loadMore() {
    setLoading(true);
    try {
      const next = await api<Page<TitleCard>>(`/home/${apiKey}?page=${more.page + 1}`);
      setMore({ page: next.page, items: [...more.items, ...next.items] });
    } finally {
      setLoading(false);
    }
  }

  if (first.error) return <Message title="Não foi possível carregar">{first.error.message}</Message>;
  if (!first.data) return <Spinner />;
  const seen = new Set<number>();
  const items = [...first.data.items, ...more.items].filter((item) => !seen.has(item.tmdbId) && seen.add(item.tmdbId));
  if (items.length === 0) return <Message title="Nada por aqui agora" />;
  return (
    <>
      <PosterGrid>
        {items.map((item) => (
          <PosterCard key={item.tmdbId} title={item} showRatings />
        ))}
      </PosterGrid>
      {more.page < first.data.totalPages ? (
        <div className="flex justify-center">
          <Button onClick={loadMore} busy={loading}>
            Carregar mais
          </Button>
        </div>
      ) : null}
    </>
  );
}

export function ExploreList() {
  const { lista } = useParams<{ lista: string }>();
  const list = findHomeList(lista);
  if (!list) return <Message title="Lista não encontrada" />;
  return (
    <div className="space-y-5">
      <div className="space-y-3">
        <Link href="/" className="inline-flex items-center gap-1 text-sm text-muted hover:text-fg">
          <ChevronLeft className="size-4" />
          Início
        </Link>
        <h1 className="text-2xl font-bold">{list.title}</h1>
        <div className="scrollbar-none -mx-4 flex gap-2 overflow-x-auto px-4">
          {homeLists.map((other) => (
            <Link
              key={other.slug}
              href={`/explorar/${other.slug}`}
              className={cx(
                "shrink-0 rounded-full px-3.5 py-1.5 text-sm font-medium",
                other.slug === list.slug ? "bg-accent text-white" : "bg-surface-2 text-muted hover:text-fg",
              )}
            >
              {other.title}
            </Link>
          ))}
        </div>
      </div>
      <ListContent key={list.api} api={list.api} />
    </div>
  );
}
