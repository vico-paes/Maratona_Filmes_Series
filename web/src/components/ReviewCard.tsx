"use client";

import Link from "next/link";
import { useState } from "react";
import { Eye, Heart } from "lucide-react";
import { api } from "@/lib/api";
import { useAuth } from "@/lib/auth";
import { formatDate, titleHref } from "@/lib/format";
import type { Review } from "@/lib/types";
import { Poster } from "./PosterCard";
import { RatingDisplay } from "./Rating";
import { Avatar, cx } from "./ui";

function LikeButton({ review }: { review: Review }) {
  const { me } = useAuth();
  const [state, setState] = useState({ liked: review.likedByMe, count: review.likes });
  const [busy, setBusy] = useState(false);

  async function toggle() {
    if (!me || busy) return;
    const next = { liked: !state.liked, count: state.count + (state.liked ? -1 : 1) };
    setState(next);
    setBusy(true);
    try {
      await api(`/reviews/${review.id}/like`, { method: next.liked ? "POST" : "DELETE" });
    } catch {
      setState(state);
    } finally {
      setBusy(false);
    }
  }

  return (
    <button
      type="button"
      onClick={toggle}
      disabled={!me}
      title={me ? undefined : "Entre para curtir"}
      className={cx("inline-flex items-center gap-1.5 rounded-lg px-2 py-1 text-xs", state.liked ? "text-danger" : "text-faint hover:text-muted", !me && "cursor-default")}
    >
      <Heart className="size-4" fill={state.liked ? "currentColor" : "none"} />
      {state.count > 0 ? state.count : null}
    </button>
  );
}

export function ReviewBody({ review, clamp }: { review: Review; clamp?: boolean }) {
  const [revealed, setRevealed] = useState(!review.spoiler);
  return (
    <div className="relative">
      <p className={cx("whitespace-pre-line break-words text-sm leading-relaxed text-fg/90", clamp && "line-clamp-5", !revealed && "select-none blur-sm")}>
        {review.body}
      </p>
      {!revealed ? (
        <button
          type="button"
          onClick={() => setRevealed(true)}
          className="absolute inset-0 flex items-center justify-center gap-2 text-sm font-medium text-fg"
        >
          <Eye className="size-4" />
          Contém spoilers. Mostrar
        </button>
      ) : null}
    </div>
  );
}

export function ReviewCard({ review, showTitle, showUser = true }: { review: Review; showTitle?: boolean; showUser?: boolean }) {
  const title = review.title;
  return (
    <article className="flex gap-3 rounded-2xl border border-line bg-surface p-4">
      {showTitle && title ? (
        <Link href={titleHref(title.mediaType, title.tmdbId)} className="w-16 shrink-0 sm:w-20">
          <Poster path={title.posterPath} name={title.name} type={title.mediaType} />
        </Link>
      ) : null}
      <div className="min-w-0 flex-1 space-y-2">
        {showTitle && title ? (
          <Link href={titleHref(title.mediaType, title.tmdbId)} className="block font-semibold hover:text-accent">
            {title.name}
            {title.year ? <span className="ml-1.5 font-normal text-faint">{title.year}</span> : null}
            {review.seasonNumber !== null ? <span className="ml-1.5 text-sm font-normal text-muted">· Temporada {review.seasonNumber}</span> : null}
          </Link>
        ) : null}
        <div className="flex flex-wrap items-center gap-x-2.5 gap-y-1 text-sm">
          {showUser ? (
            <Link href={`/u/${review.user.username}`} className="flex items-center gap-2 hover:text-accent">
              <Avatar url={review.user.avatarUrl} name={review.user.displayName} size={26} />
              <span className="font-medium">{review.user.displayName}</span>
            </Link>
          ) : null}
          <RatingDisplay rating={review.rating} style={review.ratingStyle} size="xs" />
          {!showTitle && review.seasonNumber !== null ? <span className="text-xs text-muted">Temporada {review.seasonNumber}</span> : null}
          <span className="text-xs text-faint">{formatDate(review.createdAt)}</span>
        </div>
        <ReviewBody review={review} clamp={showTitle} />
        <LikeButton review={review} />
      </div>
    </article>
  );
}
