"use client";

import { useState } from "react";
import { Star, X } from "lucide-react";
import { formatNumber } from "@/lib/format";
import type { Ratings, RatingStyle } from "@/lib/types";
import { cx } from "./ui";

function StarRow({ value, size }: { value: number; size: string }) {
  return (
    <span className="inline-flex items-center">
      {[1, 2, 3, 4, 5].map((n) => {
        const fill = value >= n ? 100 : value >= n - 0.5 ? 50 : 0;
        return (
          <span key={n} className={cx("relative inline-block", size)}>
            <Star className={cx("absolute inset-0 text-surface-3", size)} fill="currentColor" strokeWidth={0} />
            {fill > 0 ? (
              <span className="absolute inset-0 overflow-hidden" style={{ width: `${fill}%` }}>
                <Star className={cx("text-accent", size)} fill="currentColor" strokeWidth={0} />
              </span>
            ) : null}
          </span>
        );
      })}
    </span>
  );
}

export function RatingDisplay({
  rating,
  style,
  size = "sm",
  className,
}: {
  rating: number | null | undefined;
  style: RatingStyle | null | undefined;
  size?: "xs" | "sm" | "md";
  className?: string;
}) {
  if (rating === null || rating === undefined || !style) return null;
  if (style === "stars") {
    const iconSize = size === "md" ? "size-5" : size === "sm" ? "size-4" : "size-3";
    return (
      <span className={cx("inline-flex", className)} title={`${formatNumber(rating)} de 5 estrelas`}>
        <StarRow value={rating} size={iconSize} />
      </span>
    );
  }
  return (
    <span
      title={`${formatNumber(rating)} de 10`}
      className={cx(
        "inline-flex items-baseline gap-0.5 rounded-md bg-accent-soft font-semibold text-fg",
        size === "md" ? "px-2 py-0.5 text-base" : size === "sm" ? "px-1.5 py-0.5 text-sm" : "px-1 text-xs",
        className,
      )}
    >
      {formatNumber(rating)}
      <span className="text-[0.7em] font-normal text-muted">/10</span>
    </span>
  );
}

export function StarInput({ value, onChange }: { value: number | null; onChange: (value: number | null) => void }) {
  const [hover, setHover] = useState<number | null>(null);
  const shown = hover ?? value ?? 0;
  return (
    <div className="inline-flex items-center" onMouseLeave={() => setHover(null)}>
      {[1, 2, 3, 4, 5].map((n) => {
        const fill = shown >= n ? 100 : shown >= n - 0.5 ? 50 : 0;
        return (
          <span key={n} className="relative inline-block size-10">
            <Star className="absolute inset-0 size-10 text-surface-3" fill="currentColor" strokeWidth={0} />
            {fill > 0 ? (
              <span className="pointer-events-none absolute inset-0 overflow-hidden" style={{ width: `${fill}%` }}>
                <Star className="size-10 text-accent" fill="currentColor" strokeWidth={0} />
              </span>
            ) : null}
            {[n - 0.5, n].map((v, i) => (
              <button
                key={v}
                type="button"
                aria-label={`${formatNumber(v)} ${v === 1 ? "estrela" : "estrelas"}`}
                onMouseEnter={() => setHover(v)}
                onClick={() => onChange(v === value ? null : v)}
                className={cx("absolute top-0 h-full w-1/2", i === 0 ? "left-0" : "right-0")}
              />
            ))}
          </span>
        );
      })}
    </div>
  );
}

export function NumericInput({ value, onChange }: { value: number | null; onChange: (value: number | null) => void }) {
  return (
    <div className="flex w-full items-center gap-4">
      <span className={cx("w-16 text-center text-3xl font-bold tabular-nums", value === null && "text-faint")}>
        {value === null ? "–" : formatNumber(value)}
      </span>
      <input
        type="range"
        min={0}
        max={10}
        step={0.5}
        value={value ?? 5}
        onChange={(e) => onChange(Number(e.target.value))}
        onClick={(e) => value === null && onChange(Number((e.target as HTMLInputElement).value))}
        aria-label="Nota de 0 a 10"
        className={cx("h-2 flex-1 cursor-pointer", value === null && "opacity-40")}
      />
    </div>
  );
}

export function RatingInput({
  value,
  style,
  onChange,
}: {
  value: number | null;
  style: RatingStyle;
  onChange: (value: number | null) => void;
}) {
  return (
    <div className="flex min-h-10 items-center gap-2">
      {style === "stars" ? <StarInput value={value} onChange={onChange} /> : <NumericInput value={value} onChange={onChange} />}
      {value !== null ? (
        <button type="button" onClick={() => onChange(null)} className="rounded-lg p-1.5 text-muted hover:bg-surface-2 hover:text-fg" aria-label="Remover nota">
          <X className="size-4" />
        </button>
      ) : null}
    </div>
  );
}

export function StyleToggle({ style, onChange }: { style: RatingStyle; onChange: (style: RatingStyle) => void }) {
  return (
    <div className="inline-flex rounded-lg bg-surface-2 p-0.5 text-xs" title="Muda o estilo só desta nota">
      {(["stars", "numeric"] as const).map((s) => (
        <button
          key={s}
          type="button"
          onClick={() => onChange(s)}
          className={cx("flex items-center gap-1 rounded-md px-2 py-1 font-medium", style === s ? "bg-surface-3 text-fg" : "text-faint hover:text-muted")}
        >
          {s === "stars" ? <Star className="size-3" fill="currentColor" strokeWidth={0} /> : null}
          {s === "stars" ? "Estrelas" : "0–10"}
        </button>
      ))}
    </div>
  );
}

export function convertRating(value: number | null, from: RatingStyle, to: RatingStyle) {
  if (value === null || from === to) return value;
  if (to === "numeric") return value * 2;
  return Math.max(0.5, Math.round(value) / 2);
}

export function ExternalRatings({ ratings, compact }: { ratings: Ratings | null | undefined; compact?: boolean }) {
  if (!ratings) return null;
  const items = [
    ratings.imdb !== null ? { label: "IMDb", value: `${formatNumber(ratings.imdb)}`, color: "bg-[#f5c518] text-black" } : null,
    ratings.rottenTomatoes !== null ? { label: "RT", value: `${ratings.rottenTomatoes}%`, color: "bg-[#fa320a] text-white" } : null,
    ratings.metacritic !== null ? { label: "MC", value: `${ratings.metacritic}`, color: "bg-[#2a8c3c] text-white" } : null,
  ].filter((i): i is { label: string; value: string; color: string } => i !== null);
  if (items.length === 0) return null;
  return (
    <div className={cx("flex flex-wrap items-center", compact ? "gap-1.5" : "gap-2")}>
      {items.map((item) => (
        <span key={item.label} className={cx("inline-flex items-center gap-1 rounded-md bg-surface-2", compact ? "pr-1.5 text-[11px]" : "pr-2 text-sm")}>
          <span className={cx("rounded-l-md font-bold", item.color, compact ? "px-1 py-px" : "px-1.5 py-0.5")}>{item.label}</span>
          <span className="font-semibold">{item.value}</span>
        </span>
      ))}
    </div>
  );
}
