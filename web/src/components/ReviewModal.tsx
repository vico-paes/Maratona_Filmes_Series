"use client";

import { useState } from "react";
import { Bookmark, Eye } from "lucide-react";
import { api, ApiError } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { todayInput, toDateInput } from "@/lib/format";
import type { MyTitleState, RatingStyle, TitleDetails } from "@/lib/types";
import { convertRating, RatingInput, StyleToggle } from "./Rating";
import { Button, cx, ErrorText, inputClass, Modal } from "./ui";

function initialForm(state: MyTitleState, seasonId: number | null, profileStyle: RatingStyle) {
  const entry = seasonId === null ? state.entry : state.seasons.find((s) => s.seasonId === seasonId);
  const review = state.reviews.find((r) => r.seasonId === seasonId) ?? null;
  return {
    watched: entry?.watched ?? false,
    watchlist: seasonId === null ? (state.entry?.inWatchlist ?? false) : false,
    rating: entry?.rating ?? null,
    style: entry?.ratingStyle ?? profileStyle,
    date: entry?.watched ? toDateInput(entry.watchedAt) : todayInput(),
    body: review?.body ?? "",
    spoiler: review?.spoiler ?? false,
    reviewId: review?.id ?? null,
  };
}

function ReviewForm({
  details,
  state,
  seasonId,
  onClose,
  onSaved,
  header,
}: {
  details: TitleDetails;
  state: MyTitleState;
  seasonId: number | null;
  onClose: () => void;
  onSaved: () => void;
  header: React.ReactNode;
}) {
  const { me } = useAuth();
  const [initial] = useState(() => initialForm(state, seasonId, me?.ratingStyle ?? "stars"));
  const [form, setForm] = useState(initial);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState("");

  const update = (patch: Partial<typeof form>) => setForm((f) => ({ ...f, ...patch }));

  async function save() {
    setBusy(true);
    setError("");
    const base = seasonId === null ? `/me/titles/${details.id}` : `/me/seasons/${seasonId}`;
    const text = form.body.trim();
    try {
      const ratingChanged = form.rating !== initial.rating || (form.rating !== null && form.style !== initial.style);
      if (ratingChanged) await api(`${base}/rating`, { method: "PUT", body: { rating: form.rating, style: form.style } });
      const reviewChanged = text !== initial.body.trim() || form.spoiler !== initial.spoiler;
      if (text && reviewChanged) {
        await api(`/me/titles/${details.id}/review`, { method: "PUT", body: { seasonId, body: text, spoiler: form.spoiler } });
      } else if (!text && initial.reviewId !== null) {
        await api(`/me/reviews/${initial.reviewId}`, { method: "DELETE" });
      }
      const dateChanged = form.watched && form.date !== initial.date;
      if (form.watched !== initial.watched || dateChanged || ratingChanged || (text && reviewChanged)) {
        await api(`${base}/watched`, { method: "PUT", body: { value: form.watched, date: form.watched ? form.date : null } });
      }
      if (seasonId === null && !form.watched && form.watchlist !== initial.watchlist) {
        await api(`${base}/watchlist`, { method: "PUT", body: { value: form.watchlist } });
      }
      onSaved();
      onClose();
    } catch (err) {
      setError(err instanceof ApiError ? err.message : "Algo deu errado.");
      setBusy(false);
    }
  }

  return (
    <Modal
      open
      onClose={onClose}
      wide
      title={header}
      footer={
        <div className="flex gap-3">
          <Button onClick={onClose} className="flex-1">
            Cancelar
          </Button>
          <Button variant="primary" onClick={save} busy={busy} className="flex-[2]">
            {initial.reviewId !== null || initial.watched ? "Salvar alterações" : form.body.trim() ? "Publicar" : "Salvar"}
          </Button>
        </div>
      }
    >
      <div className="space-y-5">
        <div className={cx("grid gap-2", seasonId === null ? "grid-cols-2" : "grid-cols-1")}>
          <Button variant={form.watched ? "active" : "secondary"} onClick={() => update({ watched: !form.watched, watchlist: false })}>
            <Eye className="size-4" />
            {form.watched ? "Assistido" : "Assisti"}
          </Button>
          {seasonId === null ? (
            <Button
              variant={form.watchlist && !form.watched ? "active" : "secondary"}
              onClick={() => update({ watchlist: !form.watchlist, watched: false })}
            >
              <Bookmark className="size-4" />
              Quero ver
            </Button>
          ) : null}
        </div>

        <div>
          <div className="mb-2 flex items-center justify-between">
            <span className="text-sm font-medium text-muted">Sua nota</span>
            <StyleToggle style={form.style} onChange={(style) => update({ style, rating: convertRating(form.rating, form.style, style) })} />
          </div>
          <RatingInput
            value={form.rating}
            style={form.style}
            onChange={(rating) => update(rating !== null ? { rating, watched: true, watchlist: false } : { rating })}
          />
        </div>

        {form.watched ? (
          <label className="block">
            <span className="mb-1.5 block text-sm font-medium text-muted">Assistido em</span>
            <input
              type="date"
              value={form.date}
              max={todayInput()}
              min="1900-01-01"
              onChange={(e) => update({ date: e.target.value || todayInput() })}
              className={cx(inputClass, "max-w-52")}
            />
          </label>
        ) : null}

        <div>
          <span className="mb-1.5 block text-sm font-medium text-muted">Review</span>
          <textarea
            value={form.body}
            onChange={(e) => update(e.target.value.trim() ? { body: e.target.value, watched: true, watchlist: false } : { body: e.target.value })}
            rows={6}
            maxLength={10000}
            placeholder="O que você achou? Sua review fica pública no seu perfil e no feed."
            className={cx(inputClass, "resize-y leading-relaxed")}
          />
          <label className="mt-2 flex w-fit cursor-pointer items-center gap-2 text-sm text-muted">
            <input
              type="checkbox"
              checked={form.spoiler}
              onChange={(e) => update({ spoiler: e.target.checked })}
              className="size-4 accent-[var(--color-accent)]"
            />
            Contém spoilers
          </label>
        </div>
        <ErrorText>{error}</ErrorText>
      </div>
    </Modal>
  );
}

export function ReviewModal({
  details,
  state,
  initialSeasonId,
  onClose,
  onSaved,
}: {
  details: TitleDetails;
  state: MyTitleState;
  initialSeasonId: number | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const [seasonId, setSeasonId] = useState(initialSeasonId);
  const header = (
    <div className="space-y-2">
      <div className="flex items-baseline gap-2">
        <span className="truncate">{details.name}</span>
        {details.year ? <span className="shrink-0 text-sm font-normal text-faint">{details.year}</span> : null}
      </div>
      {details.mediaType === "tv" && details.seasons.length > 0 ? (
        <select
          value={seasonId ?? ""}
          onChange={(e) => setSeasonId(e.target.value ? Number(e.target.value) : null)}
          className="rounded-lg border border-line bg-surface-2 px-2.5 py-1.5 text-sm font-normal focus:border-accent focus:outline-none"
        >
          <option value="">Série inteira</option>
          {details.seasons.map((s) => (
            <option key={s.id} value={s.id}>
              {s.seasonNumber === 0 ? s.name : `Temporada ${s.seasonNumber}`}
            </option>
          ))}
        </select>
      ) : null}
    </div>
  );
  return <ReviewForm key={seasonId ?? "title"} details={details} state={state} seasonId={seasonId} onClose={onClose} onSaved={onSaved} header={header} />;
}
