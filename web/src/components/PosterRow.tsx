"use client";

import { useRef } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";

export function PosterRow({ children }: { children: React.ReactNode }) {
  const ref = useRef<HTMLDivElement>(null);

  function scroll(direction: 1 | -1) {
    const row = ref.current;
    if (row) row.scrollBy({ left: direction * row.clientWidth * 0.85, behavior: "smooth" });
  }

  const arrow =
    "absolute top-[38%] z-10 hidden size-10 -translate-y-1/2 items-center justify-center rounded-full border border-line bg-surface/90 text-fg shadow-xl backdrop-blur transition hover:bg-accent hover:text-white md:flex md:opacity-0 md:group-hover/row:opacity-100";

  return (
    <div className="group/row relative">
      <button type="button" onClick={() => scroll(-1)} aria-label="Voltar" className={`${arrow} -left-3`}>
        <ChevronLeft className="size-5" />
      </button>
      <div ref={ref} className="scrollbar-none -mx-4 flex snap-x gap-3 overflow-x-auto scroll-px-4 px-4 pb-2 [&>*]:w-32 [&>*]:shrink-0 [&>*]:snap-start sm:[&>*]:w-36">
        {children}
      </div>
      <button type="button" onClick={() => scroll(1)} aria-label="Avançar" className={`${arrow} -right-3`}>
        <ChevronRight className="size-5" />
      </button>
    </div>
  );
}
