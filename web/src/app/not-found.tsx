import Link from "next/link";

export default function NotFound() {
  return (
    <div className="mx-auto flex max-w-md flex-col items-center gap-3 py-20 text-center">
      <p className="text-5xl font-bold text-accent">404</p>
      <p className="text-lg font-semibold">Página não encontrada</p>
      <Link href="/" className="mt-2 rounded-xl bg-accent px-5 py-2.5 text-sm font-medium text-white hover:bg-accent-strong">
        Voltar ao início
      </Link>
    </div>
  );
}
