import { BrandLogo } from './BrandLogo';

export function BrandHeaderLogo({ collapsed = false }: { collapsed?: boolean }) {
  return (
    <BrandLogo
      variant={collapsed ? 'mark' : 'primary'}
      className={collapsed ? 'h-8 w-8 object-contain' : 'h-8 w-auto object-contain'}
    />
  );
}
