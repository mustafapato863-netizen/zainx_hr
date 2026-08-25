import type { ImgHTMLAttributes } from 'react';

export type BrandLogoVariant = 'primary' | 'mark' | 'wordmark' | 'app';

export interface BrandLogoProps extends Omit<ImgHTMLAttributes<HTMLImageElement>, 'src'> {
  variant?: BrandLogoVariant;
}

const sources: Record<BrandLogoVariant, string> = {
  primary: '/brand/logos/zainx-hr-primary-lockup.webp',
  mark: '/brand/logos/zainx-hr-mark.webp',
  wordmark: '/brand/logos/zainx-hr-wordmark.webp',
  app: '/brand/icons/zainx-hr-app-icon-approved.png',
};

export function BrandLogo({ variant = 'primary', alt = 'Zain X HR', ...props }: BrandLogoProps) {
  return <img src={sources[variant]} alt={alt} decoding="async" {...props} />;
}
