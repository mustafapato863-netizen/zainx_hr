import { BrandLogo } from '../react/BrandLogo';

export function ZainXBrandHero() {
  return (
    <section className="mx-auto flex min-h-[70vh] max-w-7xl items-center px-6 py-16">
      <div className="max-w-3xl">
        <BrandLogo variant="primary" className="mb-10 h-auto w-full max-w-3xl" />
        <p className="max-w-2xl text-lg text-slate-600">
          People at the center. Intelligence in every process. Enterprise control without complexity.
        </p>
      </div>
    </section>
  );
}
