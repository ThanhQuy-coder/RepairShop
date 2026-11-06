export const FIXORA_PLACEHOLDER_IMAGE = '/images/fixora-placeholder.svg';

const DEVICE_TYPE_DEFAULT_IMAGE: Record<string, string> = {
  Phone: '/images/services/phone-screen.svg',
  Laptop: '/images/services/laptop-cleaning.svg',
  Tablet: '/images/services/tablet-screen.svg',
  Smartwatch: '/images/services/watch-battery.svg',
  Electronics: '/images/services/electronics-mainboard.svg',
};

export function resolveServiceImageUrl(imageUrl?: string | null, deviceType?: string | null): string {
  const trimmed = imageUrl?.trim();
  if (!trimmed) {
    if (deviceType && DEVICE_TYPE_DEFAULT_IMAGE[deviceType]) {
      return DEVICE_TYPE_DEFAULT_IMAGE[deviceType];
    }
    return FIXORA_PLACEHOLDER_IMAGE;
  }

  if (trimmed.startsWith('http://') || trimmed.startsWith('https://') || trimmed.startsWith('blob:') || trimmed.startsWith('data:')) {
    return trimmed;
  }

  if (trimmed.startsWith('/uploads/')) {
    const apiBase = import.meta.env.VITE_API_BASE_URL as string | undefined;
    if (apiBase) {
      const origin = apiBase.replace(/\/api\/?$/, '');
      return `${origin}${trimmed}`;
    }
  }

  return trimmed.startsWith('/') ? trimmed : `/${trimmed}`;
}

export function handleServiceImageError(e: React.SyntheticEvent<HTMLImageElement, Event>): void {
  const img = e.currentTarget;
  if (img.dataset.fallbackApplied === 'true') return;
  img.dataset.fallbackApplied = 'true';
  img.src = FIXORA_PLACEHOLDER_IMAGE;
}
