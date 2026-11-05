import type { Metadata } from "next";
import { Suspense } from "react";
import { Spinner } from "@/components/ui";
import { SearchResults } from "./SearchResults";

export const metadata: Metadata = { title: "Busca" };

export default function Page() {
  return (
    <Suspense fallback={<Spinner />}>
      <SearchResults />
    </Suspense>
  );
}
