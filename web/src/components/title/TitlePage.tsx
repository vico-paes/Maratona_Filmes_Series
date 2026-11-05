"use client";

import Link from "next/link";
import { useParams } from "next/navigation";
import { useCallback, useRef, useState } from "react";
import { Bookmark, Eye, ListPlus, Lock, PenLine } from "lucide-react";
import { api, query } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { formatDate, formatRuntime, formatVotes, imageUrl, plural } from "@/lib/format";
import { useApi } from "@/lib/hooks";
import type { Entry, MediaType, MyTitleState, Note, Page, Review, TitleDetails } from "@/lib/types";
import { AwardBadge, AwardIcon } from "../AwardBadge";
import { ListPicker } from "../ListPicker";
import { NoteDrawer, type NoteTarget } from "../NoteDrawer";
import { Poster } from "../PosterCard";
import { ExternalRatings, RatingDisplay, RatingInput } from "../Rating";
import { ReviewCard } from "../ReviewCard";
import { ReviewModal } from "../ReviewModal";
import { Button, cx, Message, SectionTitle, Spinner } from "../ui";
import { SeasonList } from "./SeasonList";

function CreditLine({ label, names }: { label: string; names: string[] }) {
  if (names.length === 0) return null;
  return (
    <div className="text-sm">
      <span className="text-faint">{label} </span>
      <span className="text-fg">{names.join(", ")}</span>
    </div>
  );
}

function AwardItem({ award }: { award: TitleDetails["awards"][number] }) {
  return (
    <li className="flex items-start gap-3 py-2">
      <AwardIcon kind={award.kind} status={award.won ? (award.isMajor ? "major" : "won") : "nominated"} className="mt-0.5 size-5 shrink-0" />
      <div className="min-w-0 text-sm">
        <p className={cx("font-medium", award.isMajor && award.won && "text-gold")}>{award.category}</p>
        <p className="text-xs text-faint">{[award.year, award.person].filter(Boolean).join(" · ")}</p>
      </div>
    </li>
  );
}

function Awards({ details }: { details: TitleDetails }) {
  const [showNominations, setShowNominations] = useState(false);
  const won = details.awards.filter((a) => a.won);
  const nominated = details.awards.filter((a) => !a.won);
  if (details.awards.length === 0) return null;
  const name = details.mediaType === "movie" ? "Oscar" : "Emmy";
  return (
    <section>
      <SectionTitle>{won.length > 0 ? `${name}s vencidos` : `Indicações ao ${name}`}</SectionTitle>
      <div className="rounded-2xl border border-line bg-surface px-4 py-2">
        <ul className="divide-y divide-line">
          {(won.length > 0 ? won : nominated).map((award, i) => (
            <AwardItem key={i} award={award} />
          ))}
        </ul>
        {won.length > 0 && nominated.length > 0 ? (
          <>
            <button type="button" onClick={() => setShowNominations(!showNominations)} className="py-2 text-sm font-medium text-accent hover:underline">
              {showNominations ? "Esconder indicações" : `Ver ${plural(nominated.length, "indicação", "indicações")} sem vitória`}
            </button>
            {showNominations ? (
              <ul className="divide-y divide-line border-t border-line">
                {nominated.map((award, i) => (
                  <AwardItem key={i} award={award} />
                ))}
              </ul>
            ) : null}
          </>
        ) : null}
      </div>
    </section>
  );
}

function Reviews({ details }: { details: TitleDetails }) {
  const [seasonId, setSeasonId] = useState<string>("");
  const { data } = useApi<Page<Review>>(`/titles/${details.id}/reviews${query({ seasonId })}`);
  return (
    <section>
      <SectionTitle
        action={
          details.mediaType === "tv" && details.seasons.length > 0 ? (
            <select
              value={seasonId}
              onChange={(e) => setSeasonId(e.target.value)}
              className="rounded-lg border border-line bg-surface-2 px-2.5 py-1.5 text-sm focus:border-accent focus:outline-none"
            >
              <option value="">Série inteira</option>
              {details.seasons.map((s) => (
                <option key={s.id} value={s.id}>
                  {s.seasonNumber === 0 ? s.name : `Temporada ${s.seasonNumber}`}
                </option>
              ))}
            </select>
          ) : null
        }
      >
        Reviews {data && data.total > 0 ? <span className="text-faint">({data.total})</span> : null}
      </SectionTitle>
      {!data ? (
        <Spinner className="py-8" />
      ) : data.items.length === 0 ? (
        <p className="rounded-2xl border border-dashed border-line px-4 py-8 text-center text-sm text-muted">Ninguém escreveu uma review ainda.</p>
      ) : (
        <div className="space-y-3">
          {data.items.map((review) => (
            <ReviewCard key={review.id} review={review} />
          ))}
        </div>
      )}
    </section>
  );
}

function Actions({
  details,
  state,
  mutate,
  onReview,
  onNote,
  onList,
}: {
  details: TitleDetails;
  state: MyTitleState;
  mutate: (update: (data: MyTitleState) => MyTitleState) => void;
  onReview: () => void;
  onNote: () => void;
  onList: () => void;
}) {
  const { me } = useAuth();
  const entry = state.entry;
  const [pending, setPending] = useState<number | null | undefined>(undefined);
  const timer = useRef<ReturnType<typeof setTimeout> | null>(null);
  const style = entry?.ratingStyle ?? me?.ratingStyle ?? "stars";
  const rating = pending !== undefined ? pending : (entry?.rating ?? null);
  const hasNote = state.notes.some((n) => n.seasonId === null && n.episodeId === null);
  const myReview = state.reviews.find((r) => r.seasonId === null);

  const apply = (updated: Entry) => mutate((s) => ({ ...s, entry: updated }));

  async function toggle(kind: "watched" | "watchlist") {
    const current = kind === "watched" ? entry?.watched : entry?.inWatchlist;
    apply(await api<Entry>(`/me/titles/${details.id}/${kind}`, { method: "PUT", body: { value: !current } }));
  }

  function changeRating(value: number | null) {
    setPending(value);
    if (timer.current) clearTimeout(timer.current);
    timer.current = setTimeout(async () => {
      try {
        apply(await api<Entry>(`/me/titles/${details.id}/rating`, { method: "PUT", body: { rating: value, style } }));
      } finally {
        setPending(undefined);
      }
    }, 500);
  }

  return (
    <div className="space-y-4 rounded-2xl border border-line bg-surface p-4">
      <div className="grid grid-cols-2 gap-2">
        <Button variant={entry?.watched ? "active" : "secondary"} onClick={() => toggle("watched")}>
          <Eye className="size-4" />
          {entry?.watched ? "Assistido" : "Assisti"}
        </Button>
        <Button variant={entry?.inWatchlist && !entry.watched ? "active" : "secondary"} onClick={() => toggle("watchlist")} disabled={entry?.watched}>
          <Bookmark className="size-4" />
          Quero ver
        </Button>
      </div>
      {entry?.watched && entry.watchedAt ? <p className="-mt-1 text-xs text-faint">Assistido em {formatDate(entry.watchedAt)}</p> : null}
      <div className="flex flex-wrap items-center justify-between gap-2">
        <span className="text-sm font-medium text-muted">Sua nota</span>
        <RatingInput value={rating} style={style} onChange={changeRating} />
      </div>
      <Button variant="primary" onClick={onReview} className="w-full">
        <PenLine className="size-4" />
        {myReview ? "Editar review" : "Escrever review"}
      </Button>
      <div className="grid grid-cols-2 gap-2">
        <Button variant={hasNote ? "active" : "secondary"} onClick={onNote}>
          <Lock className="size-4" />
          Nota
        </Button>
        <Button onClick={onList}>
          <ListPlus className="size-4" />
          Lista
        </Button>
      </div>
    </div>
  );
}

export function TitlePage({ type }: { type: MediaType }) {
  const params = useParams<{ tmdbId: string }>();
  const tmdbId = Number(params.tmdbId);
  const { me, ready } = useAuth();
  const details = useApi<TitleDetails>(Number.isInteger(tmdbId) && tmdbId > 0 ? `/titles/${type}/${tmdbId}` : null);
  const data = details.data;
  const state = useApi<MyTitleState>(me && data ? `/me/titles/${data.id}` : null);
  const [reviewSeason, setReviewSeason] = useState<number | null | undefined>(undefined);
  const [note, setNote] = useState<NoteTarget | null>(null);
  const [listOpen, setListOpen] = useState(false);
  const mutateState = state.mutate;

  const onNoteSaved = useCallback(
    (saved: Note | null) => {
      if (!note) return;
      mutateState((s) => ({
        ...s,
        notes: [...s.notes.filter((n) => !(n.seasonId === note.seasonId && n.episodeId === note.episodeId)), ...(saved ? [saved] : [])],
      }));
    },
    [note, mutateState],
  );

  if (details.error) return <Message title={details.error.status === 404 ? "Título não encontrado" : "Não foi possível carregar"}>{details.error.message}</Message>;
  if (!data) return <Spinner />;

  const backdrop = imageUrl(data.backdropPath, "w1280");
  const people = data.mediaType === "movie" ? data.credits.directors : data.credits.creators;
  const facts = [
    data.year,
    data.mediaType === "movie" ? formatRuntime(data.runtime) : data.seasons.filter((s) => s.seasonNumber > 0).length ? plural(data.seasons.filter((s) => s.seasonNumber > 0).length, "temporada", "temporadas") : null,
    data.mediaType === "tv" && data.runtime ? `${formatRuntime(data.runtime)} por episódio` : null,
  ].filter(Boolean);
  const noteFor = (target: NoteTarget) => state.data?.notes.find((n) => n.seasonId === target.seasonId && n.episodeId === target.episodeId)?.body ?? "";

  return (
    <div className="space-y-8">
      <title>{`${data.name} · Maratona`}</title>
      <div className="relative -mx-4 -mt-6">
        <div className="relative h-52 overflow-hidden sm:h-80">
          {backdrop ? <img src={backdrop} alt="" className="size-full object-cover object-top opacity-60" /> : <div className="size-full bg-gradient-to-br from-accent-soft to-surface" />}
          <div className="absolute inset-0 bg-gradient-to-t from-bg via-bg/50 to-transparent" />
        </div>
        <div className="relative -mt-28 flex gap-4 px-4 sm:-mt-40 sm:gap-6">
          <div className="relative w-28 shrink-0 sm:w-48">
            <Poster path={data.posterPath} name={data.name} type={data.mediaType} className="shadow-2xl ring-1 ring-white/10" />
          </div>
          <div className="min-w-0 flex-1 self-end pb-1">
            <p className="text-xs font-semibold uppercase tracking-wider text-accent">{data.mediaType === "movie" ? "Filme" : "Série"}</p>
            <h1 className="text-2xl font-bold leading-tight sm:text-4xl">{data.name}</h1>
            {data.originalName && data.originalName !== data.name ? <p className="text-sm text-faint">{data.originalName}</p> : null}
            <p className="mt-1 text-sm text-muted">{facts.join(" · ")}</p>
            {data.genres.length ? <p className="mt-1 hidden text-sm text-faint sm:block">{data.genres.join(", ")}</p> : null}
            <div className="mt-3 flex flex-wrap items-center gap-2">
              <AwardBadge badge={data.award} size="md" />
              <ExternalRatings ratings={data.ratings} />
            </div>
          </div>
        </div>
      </div>

      <div className="grid gap-8 lg:grid-cols-[1fr_340px]">
        <div className="space-y-8">
          {data.genres.length ? <p className="-mt-4 text-sm text-faint sm:hidden">{data.genres.join(", ")}</p> : null}
          <section className="space-y-3">
            {data.overview ? <p className="leading-relaxed text-fg/90">{data.overview}</p> : <p className="text-muted">Sem sinopse disponível.</p>}
            <div className="space-y-1">
              <CreditLine label={data.mediaType === "movie" ? "Direção" : "Criação"} names={people} />
              <CreditLine label="Roteiro" names={data.credits.writers} />
              {data.ratings?.imdbVotes ? <p className="text-xs text-faint">{formatVotes(data.ratings.imdbVotes)} votos no IMDb</p> : null}
            </div>
          </section>

          {data.credits.cast.length > 0 ? (
            <section>
              <SectionTitle>Elenco principal</SectionTitle>
              <div className="scrollbar-none -mx-4 flex gap-3 overflow-x-auto px-4">
                {data.credits.cast.map((person) => {
                  const photo = imageUrl(person.profilePath, "w185");
                  return (
                    <div key={person.name} className="w-24 shrink-0 text-center">
                      <div className="mx-auto size-20 overflow-hidden rounded-full bg-surface-3">
                        {photo ? <img src={photo} alt="" loading="lazy" className="size-full object-cover" /> : null}
                      </div>
                      <p className="mt-2 line-clamp-2 text-xs font-medium">{person.name}</p>
                      {person.character ? <p className="line-clamp-2 text-[11px] text-faint">{person.character}</p> : null}
                    </div>
                  );
                })}
              </div>
            </section>
          ) : null}

          {data.mediaType === "tv" && data.seasons.length > 0 ? (
            <section>
              <SectionTitle>Temporadas</SectionTitle>
              <SeasonList details={data} state={state.data} onChanged={state.reload} onReview={(id) => setReviewSeason(id)} onNote={setNote} />
            </section>
          ) : null}

          <Awards details={data} />
          <Reviews key={state.data?.reviews.map((r) => r.updatedAt).join() ?? ""} details={data} />
        </div>

        <aside className="lg:sticky lg:top-24 lg:self-start">
          {!ready ? null : !me ? (
            <div className="rounded-2xl border border-line bg-surface p-5 text-center">
              <p className="font-medium">Já assistiu?</p>
              <p className="mt-1 text-sm text-muted">Entre para marcar, dar nota e escrever sua review.</p>
              <Link href="/entrar" className="mt-4 inline-block rounded-xl bg-accent px-5 py-2.5 text-sm font-medium text-white hover:bg-accent-strong">
                Entrar
              </Link>
            </div>
          ) : !state.data ? (
            <Spinner className="py-10" />
          ) : (
            <Actions
              details={data}
              state={state.data}
              mutate={state.mutate}
              onReview={() => setReviewSeason(null)}
              onNote={() => setNote({ titleId: data.id, seasonId: null, episodeId: null, label: data.name })}
              onList={() => setListOpen(true)}
            />
          )}
          {state.data?.reviews.length ? (
            <div className="mt-4 space-y-2 rounded-2xl border border-line bg-surface p-4">
              <p className="text-sm font-medium text-muted">Suas reviews</p>
              {state.data.reviews.map((r) => (
                <button key={r.id} type="button" onClick={() => setReviewSeason(r.seasonId)} className="block w-full rounded-xl bg-surface-2 p-3 text-left hover:bg-surface-3">
                  <span className="mb-1 flex items-center gap-2 text-xs text-faint">
                    {r.seasonNumber !== null ? `Temporada ${r.seasonNumber}` : data.mediaType === "tv" ? "Série inteira" : "Filme"}
                    <RatingDisplay rating={r.rating} style={r.ratingStyle} size="xs" />
                  </span>
                  <span className="line-clamp-2 text-sm">{r.body}</span>
                </button>
              ))}
            </div>
          ) : null}
        </aside>
      </div>

      {reviewSeason !== undefined && state.data ? (
        <ReviewModal details={data} state={state.data} initialSeasonId={reviewSeason} onClose={() => setReviewSeason(undefined)} onSaved={state.reload} />
      ) : null}
      {note ? <NoteDrawer key={`${note.seasonId}-${note.episodeId}`} target={note} initial={noteFor(note)} onSaved={onNoteSaved} onClose={() => setNote(null)} /> : null}
      {listOpen ? <ListPicker titleId={data.id} titleName={data.name} onClose={() => setListOpen(false)} /> : null}
    </div>
  );
}
