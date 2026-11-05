"use client";

import { useState } from "react";
import { ReviewFeed } from "@/components/ReviewFeed";
import { LoginPrompt, Segmented, Spinner } from "@/components/ui";
import { useAuth } from "@/lib/auth";

export default function FeedPage() {
  const { me, ready } = useAuth();
  const [scope, setScope] = useState<"all" | "following">("all");

  return (
    <div className="mx-auto max-w-2xl space-y-5">
      <div className="flex flex-wrap items-center justify-between gap-3">
        <h1 className="text-2xl font-bold">Feed</h1>
        <Segmented
          value={scope}
          onChange={setScope}
          options={[
            { value: "all", label: "Todos" },
            { value: "following", label: "Seguindo" },
          ]}
        />
      </div>
      {scope === "all" ? (
        <ReviewFeed path="/feed?scope=all" empty="Nenhuma review ainda." />
      ) : !ready ? (
        <Spinner />
      ) : !me ? (
        <LoginPrompt text="Entre para ver as reviews de quem você segue." />
      ) : (
        <ReviewFeed path="/feed?scope=following" empty="Quem você segue ainda não escreveu reviews." />
      )}
    </div>
  );
}
