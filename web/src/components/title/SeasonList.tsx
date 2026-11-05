"use client";

import { useState } from "react";
import { ChevronDown, Eye, Lock, PenLine } from "lucide-react";
import { api } from "@/lib/api";
import { formatDate, formatRuntime, imageUrl } from "@/lib/format";
import { useApi } from "@/lib/hooks";
import type { MyTitleState, SeasonDetails, SeasonSummary, TitleDetails } from "@/lib/types";
import type { NoteTarget } from "../NoteDrawer";
import { RatingDisplay } from "../Rating";
import { cx, Spinner } from "../ui";

function Episodes({
  details,
  season,
  state,
  onNote,
}: {
  details: TitleDetails;
  season: SeasonSummary;
  state: MyTitleState | undefined;
  onNote: (target: NoteTarget) => void;
}) {
  const { data, error } = useApi<SeasonDetails>(`/titles/tv/${details.tmdbId}/seasons/${season.seasonNumber}`);
  if (error) return <p className="px-4 py-6 text-center text-sm text-muted">{error.message}</p>;
  if (!data) return <Spinner className="py-8" />;
  if (data.episodes.length === 0) return <p className="px-4 py-6 text-center text-sm text-muted">Nenhum episódio anunciado ainda.</p>;
  return (
    <ol className="divide-y divide-line">
      {data.episodes.map((episode) => {
        const hasNote = state?.notes.some((n) => n.episodeId === episode.id);
        const still = imageUrl(episode.stillPath, "w185");
        return (
          <li key={episode.id} className="flex gap-3 px-4 py-3">
            <div className="hidden aspect-video w-32 shrink-0 overflow-hidden rounded-lg bg-surface-3 sm:block">
              {still ? <img src={still} alt="" loading="lazy" className="size-full object-cover" /> : null}
            </div>
            <div className="min-w-0 flex-1">
              <p className="text-sm font-medium">
                <span className="mr-1.5 text-faint">{episode.episodeNumber}.</span>
                {episode.name}
              </p>
              <p className="text-xs text-faint">
                {[formatDate(episode.airDate), formatRuntime(episode.runtime)].filter(Boolean).join(" · ")}
              </p>
              {episode.overview ? <p className="mt-1 line-clamp-2 text-xs text-muted">{episode.overview}</p> : null}
            </div>
            {state ? (
              <button
                type="button"
                onClick={() =>
                  onNote({
                    titleId: details.id,
                    seasonId: season.id,
                    episodeId: episode.id,
                    label: `T${season.seasonNumber} E${episode.episodeNumber}`,
                    subtitle: episode.name,
                  })
                }
                title={hasNote ? "Ver sua nota deste episódio" : "Escrever nota privada"}
                className={cx(
                  "flex h-9 shrink-0 items-center gap-1.5 self-center rounded-lg px-2.5 text-xs font-medium",
                  hasNote ? "bg-accent-soft text-accent" : "text-faint hover:bg-surface-3 hover:text-muted",
                )}
              >
                <Lock className="size-3.5" />
                Nota
              </button>
            ) : null}
          </li>
        );
      })}
    </ol>
  );
}

export function SeasonList({
  details,
  state,
  onChanged,
  onReview,
  onNote,
}: {
  details: TitleDetails;
  state: MyTitleState | undefined;
  onChanged: () => void;
  onReview: (seasonId: number) => void;
  onNote: (target: NoteTarget) => void;
}) {
  const [open, setOpen] = useState<number | null>(null);
  const [busy, setBusy] = useState<number | null>(null);

  async function toggleWatched(seasonId: number, watched: boolean) {
    setBusy(seasonId);
    try {
      await api(`/me/seasons/${seasonId}/watched`, { method: "PUT", body: { value: !watched } });
      onChanged();
    } finally {
      setBusy(null);
    }
  }

  return (
    <div className="space-y-2">
      {details.seasons.map((season) => {
        const entry = state?.seasons.find((s) => s.seasonId === season.id);
        const watched = entry?.watched ?? false;
        const poster = imageUrl(season.posterPath, "w92");
        const isOpen = open === season.id;
        return (
          <div key={season.id} className="overflow-hidden rounded-2xl border border-line bg-surface">
            <div className="flex items-center gap-3 p-3">
              <button type="button" onClick={() => setOpen(isOpen ? null : season.id)} className="flex min-w-0 flex-1 items-center gap-3 text-left">
                <div className="aspect-[2/3] w-11 shrink-0 overflow-hidden rounded-md bg-surface-3">
                  {poster ? <img src={poster} alt="" loading="lazy" className="size-full object-cover" /> : null}
                </div>
                <div className="min-w-0 flex-1">
                  <p className="truncate font-medium">{season.name}</p>
                  <p className="flex flex-wrap items-center gap-x-2 text-xs text-faint">
                    <span>{season.episodeCount} episódios</span>
                    {season.airDate ? <span>{new Date(`${season.airDate}T12:00:00`).getFullYear()}</span> : null}
                    {entry?.watchedAt && watched ? <span className="text-muted">Visto em {formatDate(entry.watchedAt)}</span> : null}
                  </p>
                  {entry?.rating !== null && entry?.rating !== undefined ? (
                    <RatingDisplay rating={entry.rating} style={entry.ratingStyle} size="xs" className="mt-1" />
                  ) : null}
                </div>
                <ChevronDown className={cx("size-5 shrink-0 text-faint transition-transform", isOpen && "rotate-180")} />
              </button>
              {state ? (
                <div className="flex shrink-0 gap-1">
                  <button
                    type="button"
                    onClick={() => onReview(season.id)}
                    title="Nota e review da temporada"
                    className="rounded-lg p-2 text-faint hover:bg-surface-3 hover:text-fg"
                  >
                    <PenLine className="size-4" />
                  </button>
                  <button
                    type="button"
                    disabled={busy === season.id}
                    onClick={() => toggleWatched(season.id, watched)}
                    title={watched ? "Desmarcar temporada" : "Marcar temporada como assistida"}
                    className={cx(
                      "rounded-lg p-2 disabled:opacity-50",
                      watched ? "bg-accent-soft text-accent" : "text-faint hover:bg-surface-3 hover:text-fg",
                    )}
                  >
                    <Eye className="size-4" />
                  </button>
                </div>
              ) : null}
            </div>
            {isOpen ? (
              <div className="border-t border-line bg-bg/40">
                <Episodes details={details} season={season} state={state} onNote={onNote} />
              </div>
            ) : null}
          </div>
        );
      })}
    </div>
  );
}
