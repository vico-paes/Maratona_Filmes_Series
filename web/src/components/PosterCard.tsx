import Link from "next/link";
import { Film, Tv } from "lucide-react";
import { imageUrl, titleHref } from "@/lib/format";
import type { TitleCard } from "@/lib/types";
import { AwardBadge } from "./AwardBadge";
import { ExternalRatings } from "./Rating";
import { cx } from "./ui";

export function Poster({ path, name, type, className }: { path: string | null; name: string; type: TitleCard["mediaType"]; className?: string }) {
  const url = imageUrl(path, "w342");
  return (
    <div className={cx("relative aspect-[2/3] overflow-hidden rounded-xl bg-surface-2", className)}>
      {url ? (
        <img src={url} alt={name} loading="lazy" className="size-full object-cover" />
      ) : (
        <div className="flex size-full flex-col items-center justify-center gap-2 p-3 text-center text-faint">
          {type === "movie" ? <Film className="size-8" /> : <Tv className="size-8" />}
          <span className="line-clamp-3 text-xs">{name}</span>
        </div>
      )}
    </div>
  );
}

export function PosterCard({
  title,
  topLeft,
  bottom,
  showRatings,
  showName = true,
  className,
}: {
  title: TitleCard;
  topLeft?: React.ReactNode;
  bottom?: React.ReactNode;
  showRatings?: boolean;
  showName?: boolean;
  className?: string;
}) {
  return (
    <Link href={titleHref(title.mediaType, title.tmdbId)} className={cx("group block min-w-0", className)}>
      <div className="relative">
        <Poster path={title.posterPath} name={title.name} type={title.mediaType} className="ring-1 ring-white/5 transition group-hover:ring-accent/70" />
        {topLeft ? <div className="absolute left-1.5 top-1.5 flex gap-1">{topLeft}</div> : null}
        <AwardBadge badge={title.award} className="absolute right-1.5 top-1.5" />
        {showRatings && title.ratings ? (
          <div className="absolute inset-x-0 bottom-0 rounded-b-xl bg-gradient-to-t from-black/90 to-transparent px-1.5 pb-1.5 pt-6">
            <ExternalRatings ratings={title.ratings} compact />
          </div>
        ) : null}
      </div>
      {showName ? (
        <div className="mt-1.5 px-0.5">
          <p className="truncate text-sm font-medium group-hover:text-accent">{title.name}</p>
          {bottom ?? <p className="text-xs text-faint">{title.year ?? "—"}</p>}
        </div>
      ) : (
        bottom
      )}
    </Link>
  );
}

export function PosterGrid({ children }: { children: React.ReactNode }) {
  return <div className="grid grid-cols-3 gap-3 sm:grid-cols-4 md:grid-cols-5 lg:grid-cols-6 xl:grid-cols-7">{children}</div>;
}
