"use client";

import { useState } from "react";
import { Bookmark, Eye, Play, Sparkles } from "lucide-react";
import { PosterCard, PosterGrid } from "@/components/PosterCard";
import { RatingDisplay } from "@/components/Rating";
import { Button, LoginPrompt, Message, Segmented, Spinner } from "@/components/ui";
import { api, query } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { formatDate } from "@/lib/format";
import { useApi } from "@/lib/hooks";
import type { LibraryItem, Page } from "@/lib/types";

const sorts = [
  { value: "unwatched", label: "Não assistidos primeiro" },
  { value: "watched", label: "Assistidos primeiro" },
  { value: "added", label: "Adicionados recentemente" },
  { value: "recent", label: "Assistidos recentemente" },
  { value: "rating_desc", label: "Minha nota (maior)" },
  { value: "rating_asc", label: "Minha nota (menor)" },
  { value: "imdb", label: "Nota IMDb" },
  { value: "year", label: "Ano" },
  { value: "az", label: "A–Z" },
];

const statuses = [
  { value: "all", label: "Tudo" },
  { value: "watchlist", label: "Quero ver" },
  { value: "watching", label: "Assistindo" },
  { value: "watched", label: "Assistidos" },
];

function Marker({ item }: { item: LibraryItem }) {
  const [Icon, label] = item.watched ? [Eye, "Assistido"] : item.watching ? [Play, "Assistindo"] : item.inWatchlist ? [Bookmark, "Quero ver"] : [null, ""];
  return (
    <>
      {Icon ? (
        <span title={label} className={item.watched ? "rounded-full bg-accent p-1.5 text-white shadow-lg" : "rounded-full bg-black/70 p-1.5 text-fg backdrop-blur"}>
          <Icon className="size-3.5" fill={item.watching ? "currentColor" : "none"} />
        </span>
      ) : null}
      {item.newSeasons > 0 ? (
        <span title="Nova temporada" className="flex items-center gap-1 rounded-full bg-gold px-1.5 py-1 text-[10px] font-bold text-black">
          <Sparkles className="size-3" />
          NOVA
        </span>
      ) : null}
    </>
  );
}

function Library() {
  const [type, setType] = useState<"all" | "movie" | "tv">("all");
  const [status, setStatus] = useState("all");
  const [sort, setSort] = useState("unwatched");
  const key = query({ type, status, sort });
  const first = useApi<Page<LibraryItem>>(`/me/library${key}`);
  const [more, setMore] = useState<{ key: string; page: number; items: LibraryItem[] }>({ key: "", page: 1, items: [] });
  const [loadingMore, setLoadingMore] = useState(false);
  const extra = more.key === key ? more : { key, page: 1, items: [] };

  async function loadMore() {
    setLoadingMore(true);
    try {
      const next = await api<Page<LibraryItem>>(`/me/library${query({ type, status, sort, page: extra.page + 1 })}`);
      setMore({ key, page: next.page, items: [...extra.items, ...next.items] });
    } finally {
      setLoadingMore(false);
    }
  }

  const items = first.data ? [...first.data.items, ...extra.items] : [];
  const hasMore = first.data ? extra.page < first.data.totalPages : false;

  return (
    <div className="space-y-5">
      <div className="flex flex-wrap items-end justify-between gap-3">
        <h1 className="text-2xl font-bold">Biblioteca</h1>
        {first.data ? <span className="text-sm text-faint">{first.data.total} títulos</span> : null}
      </div>
      <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-center">
        <Segmented
          value={type}
          onChange={setType}
          options={[
            { value: "all", label: "Todos" },
            { value: "movie", label: "Filmes" },
            { value: "tv", label: "Séries" },
          ]}
        />
        <div className="grid grid-cols-2 gap-2 sm:flex">
          <select
            value={status}
            onChange={(e) => setStatus(e.target.value)}
            aria-label="Mostrar"
            className="h-10 rounded-xl border border-line bg-surface-2 px-3 text-sm focus:border-accent focus:outline-none"
          >
            {statuses.map((s) => (
              <option key={s.value} value={s.value}>
                {s.label}
              </option>
            ))}
          </select>
          <select
            value={sort}
            onChange={(e) => setSort(e.target.value)}
            aria-label="Ordenar"
            className="h-10 rounded-xl border border-line bg-surface-2 px-3 text-sm focus:border-accent focus:outline-none"
          >
            {sorts.map((s) => (
              <option key={s.value} value={s.value}>
                {s.label}
              </option>
            ))}
          </select>
        </div>
      </div>

      {first.error ? (
        <Message title="Não foi possível carregar">{first.error.message}</Message>
      ) : !first.data ? (
        <Spinner />
      ) : items.length === 0 ? (
        <Message title="Nada por aqui ainda">Busque um filme ou série e marque como assistido ou “Quero ver”.</Message>
      ) : (
        <>
          <PosterGrid>
            {items.map((item) => (
              <PosterCard
                key={item.titleId}
                title={item.title}
                topLeft={<Marker item={item} />}
                bottom={
                  <div className="flex h-5 items-center gap-2 text-xs text-faint">
                    {item.rating !== null ? (
                      <RatingDisplay rating={item.rating} style={item.ratingStyle} size="xs" />
                    ) : item.watched && item.watchedAt ? (
                      formatDate(item.watchedAt)
                    ) : (
                      item.title.year
                    )}
                  </div>
                }
              />
            ))}
          </PosterGrid>
          {hasMore ? (
            <div className="flex justify-center">
              <Button onClick={loadMore} busy={loadingMore}>
                Carregar mais
              </Button>
            </div>
          ) : null}
        </>
      )}
    </div>
  );
}

export default function LibraryPage() {
  const { me, ready } = useAuth();
  if (!ready) return <Spinner />;
  if (!me) return <LoginPrompt text="Sua biblioteca guarda tudo que você assistiu e quer ver." />;
  return <Library />;
}
