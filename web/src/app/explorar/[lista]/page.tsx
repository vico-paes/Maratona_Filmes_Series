import type { Metadata } from "next";
import { findHomeList } from "@/lib/lists";
import { ExploreList } from "./ExploreList";

export async function generateMetadata({ params }: PageProps<"/explorar/[lista]">): Promise<Metadata> {
  const { lista } = await params;
  return { title: findHomeList(lista)?.title ?? "Lista" };
}

export default function Page() {
  return <ExploreList />;
}
