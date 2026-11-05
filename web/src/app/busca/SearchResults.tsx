"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useState } from "react";
import { PosterCard, PosterGrid } from "@/components/PosterCard";
import { Avatar, Message, Segmented, Spinner } from "@/components/ui";
import { query } from "@/lib/api";
import { useApi } from "@/lib/hooks";
import type { TitleCard, UserSummary } from "@/lib/types";

type Tab = "movies" | "series" | "people";

export function SearchResults() {
  const q = useSearchParams().get("q")?.trim() ?? "";
  const [tab, setTab] = useState<Tab>("movies");
  const titles = useApi<{ movies: TitleCard[]; series: TitleCard[] }>(q ? `/titles/search${query({ q })}` : null);
  const people = useApi<UserSummary[]>(q ? `/users/search${query({ q: q.slice(0, 50) })}` : null);

  if (!q) return <Message title="Busque um filme, série ou pessoa">Use a barra de busca no topo.</Message>;

  const counts = { movies: titles.data?.movies.length ?? 0, series: titles.data?.series.length ?? 0, people: people.data?.length ?? 0 };

  return (
    <div className="space-y-5">
      <h1 className="text-xl font-semibold">
        Resultados para <span className="text-accent">“{q}”</span>
      </h1>
      <Segmented
        value={tab}
        onChange={setTab}
        options={[
          { value: "movies", label: `Filmes${titles.data ? ` (${counts.movies})` : ""}` },
          { value: "series", label: `Séries${titles.data ? ` (${counts.series})` : ""}` },
          { value: "people", label: `Pessoas${people.data ? ` (${counts.people})` : ""}` },
        ]}
      />
      {tab === "people" ? (
        people.loading ? (
          <Spinner />
        ) : counts.people === 0 ? (
          <Message title="Ninguém encontrado" />
        ) : (
          <div className="grid gap-2 sm:grid-cols-2">
            {people.data!.map((user) => (
              <Link key={user.username} href={`/u/${user.username}`} className="flex items-center gap-3 rounded-xl bg-surface p-3 hover:bg-surface-2">
                <Avatar url={user.avatarUrl} name={user.displayName} size={44} />
                <div className="min-w-0">
                  <p className="truncate font-medium">{user.displayName}</p>
                  <p className="truncate text-sm text-faint">@{user.username}</p>
                </div>
              </Link>
            ))}
          </div>
        )
      ) : titles.error ? (
        <Message title="Não foi possível buscar">{titles.error.message}</Message>
      ) : titles.loading ? (
        <Spinner />
      ) : counts[tab] === 0 ? (
        <Message title={tab === "movies" ? "Nenhum filme encontrado" : "Nenhuma série encontrada"} />
      ) : (
        <PosterGrid>
          {titles.data![tab].map((item) => (
            <PosterCard key={item.tmdbId} title={item} showRatings />
          ))}
        </PosterGrid>
      )}
    </div>
  );
}
