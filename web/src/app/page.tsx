"use client";

import Link from "next/link";
import { ChevronRight, Sparkles } from "lucide-react";
import { PosterCard } from "@/components/PosterCard";
import { PosterRow } from "@/components/PosterRow";
import { Message, SectionTitle, Spinner } from "@/components/ui";
import { useAuth } from "@/lib/auth";
import { formatDate } from "@/lib/format";
import { useApi } from "@/lib/hooks";
import { homeLists } from "@/lib/lists";
import type { Home, NewSeason, TitleCard } from "@/lib/types";

function Row({ title, items, href }: { title: string; items: TitleCard[]; href: string }) {
  if (items.length === 0) return null;
  return (
    <section>
      <SectionTitle
        action={
          <Link href={href} className="flex items-center gap-0.5 text-sm font-medium text-accent hover:underline">
            Ver mais
            <ChevronRight className="size-4" />
          </Link>
        }
      >
        {title}
      </SectionTitle>
      <PosterRow>
        {items.map((item) => (
          <PosterCard key={`${item.mediaType}-${item.tmdbId}`} title={item} showRatings />
        ))}
      </PosterRow>
    </section>
  );
}

function NewSeasons() {
  const { me } = useAuth();
  const { data } = useApi<NewSeason[]>(me ? "/me/new-seasons" : null);
  if (!data || data.length === 0) return null;
  return (
    <section className="rounded-2xl border border-accent/30 bg-gradient-to-br from-accent-soft/60 to-surface p-4">
      <SectionTitle>
        <Sparkles className="size-5 text-accent" />
        Novas temporadas
      </SectionTitle>
      <PosterRow>
        {data.map((item) => (
          <PosterCard
            key={item.seasonId}
            title={item.title}
            topLeft={<span className="rounded-full bg-accent px-2 py-0.5 text-[11px] font-semibold text-white">T{item.seasonNumber}</span>}
            bottom={
              <p className="text-xs text-muted">
                {item.released ? `Saiu em ${formatDate(item.airDate)}` : `Estreia em ${formatDate(item.airDate)}`}
              </p>
            }
          />
        ))}
      </PosterRow>
    </section>
  );
}

export default function HomePage() {
  const { me, ready } = useAuth();
  const { data, error } = useApi<Home>("/home");

  return (
    <div className="space-y-8">
      {ready && !me ? (
        <section className="rounded-2xl border border-line bg-gradient-to-br from-accent-soft to-surface p-6 sm:p-8">
          <h1 className="text-2xl font-bold sm:text-3xl">Tudo que você assiste, num lugar só.</h1>
          <p className="mt-2 max-w-xl text-muted">
            Marque filmes e séries como assistidos, dê notas, escreva reviews, anote o que achou de cada episódio e acompanhe o que seus amigos estão vendo.
          </p>
          <div className="mt-5 flex gap-3">
            <Link href="/cadastro" className="rounded-xl bg-accent px-5 py-2.5 text-sm font-medium text-white hover:bg-accent-strong">
              Criar conta
            </Link>
            <Link href="/entrar" className="rounded-xl bg-surface-3 px-5 py-2.5 text-sm font-medium hover:bg-line">
              Entrar
            </Link>
          </div>
        </section>
      ) : null}
      <NewSeasons />
      {error ? (
        <Message title="Não foi possível carregar">{error.message}</Message>
      ) : !data ? (
        <Spinner />
      ) : (
        homeLists.map((list) => <Row key={list.slug} title={list.title} items={data[list.key]} href={`/explorar/${list.slug}`} />)
      )}
    </div>
  );
}
