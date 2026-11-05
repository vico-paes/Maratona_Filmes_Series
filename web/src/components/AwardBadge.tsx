import type { AwardBadge as Badge, AwardKind } from "@/lib/types";
import { cx } from "./ui";

function OscarShape() {
  return (
    <>
      <circle cx="12" cy="3.4" r="1.9" />
      <path d="M9.4 6.2h5.2l-.7 5.9-.5 6.4h-2.8l-.5-6.4z" />
      <rect x="8.2" y="19" width="7.6" height="1.9" rx=".5" />
      <rect x="7" y="21.2" width="10" height="1.8" rx=".5" />
    </>
  );
}

function EmmyShape() {
  return (
    <>
      <ellipse cx="12" cy="3.2" rx="3" ry="1.4" />
      <path d="M11.3 6.6c-2.6-.6-5-2-6.6-4 .2 3.6 2.6 6.4 6.2 7.4z" />
      <path d="M12.7 6.6c2.6-.6 5-2 6.6-4-.2 3.6-2.6 6.4-6.2 7.4z" />
      <path d="M11 6.2h2l-.3 12.3h-1.4z" />
      <rect x="8.2" y="19" width="7.6" height="1.9" rx=".5" />
      <rect x="7" y="21.2" width="10" height="1.8" rx=".5" />
    </>
  );
}

export function AwardIcon({ kind, status, className }: { kind: AwardKind; status: Badge["status"]; className?: string }) {
  const filled = status !== "nominated";
  return (
    <svg
      viewBox="0 0 24 24"
      aria-hidden
      className={cx(filled ? "fill-gold text-gold" : "fill-none text-gold-deep", status === "major" && "award-glow", className)}
      stroke="currentColor"
      strokeWidth={filled ? 0.6 : 1.3}
      strokeLinejoin="round"
    >
      {kind === "oscar" ? <OscarShape /> : <EmmyShape />}
    </svg>
  );
}

export function awardLabel(badge: Badge) {
  const name = badge.kind === "oscar" ? "Oscar" : "Emmy";
  const names = badge.kind === "oscar" ? "Oscars" : "Emmys";
  const nominations = badge.nominations > 0 ? ` (${badge.nominations} ${badge.nominations === 1 ? "indicação" : "indicações"})` : "";
  if (badge.status === "nominated") return `Indicado ao ${name}${nominations}`;
  const wins = `Venceu ${badge.wins} ${badge.wins === 1 ? name : names}${nominations}`;
  return badge.status === "major" ? `${wins}, incluindo categoria principal` : wins;
}

export function AwardBadge({ badge, size = "sm", className }: { badge: Badge | null | undefined; size?: "sm" | "md" | "lg"; className?: string }) {
  if (!badge) return null;
  const icon = size === "lg" ? "size-7" : size === "md" ? "size-5" : "size-4";
  return (
    <span
      title={awardLabel(badge)}
      aria-label={awardLabel(badge)}
      className={cx(
        "inline-flex items-center gap-0.5 rounded-full bg-black/65 font-semibold backdrop-blur",
        size === "lg" ? "px-2.5 py-1 text-base" : size === "md" ? "px-2 py-0.5 text-sm" : "px-1.5 py-0.5 text-xs",
        badge.status === "nominated" ? "text-gold-deep" : "text-gold",
        className,
      )}
    >
      <AwardIcon kind={badge.kind} status={badge.status} className={icon} />
      {badge.status !== "nominated" && badge.wins > 0 ? <span>{badge.wins}</span> : null}
    </span>
  );
}
