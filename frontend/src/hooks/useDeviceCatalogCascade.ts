import { useEffect, useState } from 'react';
import { deviceService } from '../services/deviceService';
import type { BrandItem, DeviceModelItem } from '../types/device.types';

export function useDeviceCatalogCascade(deviceType: string, selectedBrandName: string) {
  const [brands, setBrands] = useState<BrandItem[]>([]);
  const [models, setModels] = useState<DeviceModelItem[]>([]);
  const [loadingBrands, setLoadingBrands] = useState(false);
  const [loadingModels, setLoadingModels] = useState(false);

  useEffect(() => {
    let cancelled = false;
    if (!deviceType) {
      setBrands([]);
      setModels([]);
      return;
    }

    setLoadingBrands(true);
    deviceService
      .getPublicBrands({ deviceType, pageSize: 100 })
      .then((res) => {
        if (!cancelled) {
          setBrands(res.items ?? []);
        }
      })
      .catch(() => {
        if (!cancelled) setBrands([]);
      })
      .finally(() => {
        if (!cancelled) setLoadingBrands(false);
      });

    return () => {
      cancelled = true;
    };
  }, [deviceType]);

  const selectedBrandObj = brands.find(
    (b) => b.name.toLowerCase() === selectedBrandName.trim().toLowerCase() || b.id === selectedBrandName
  );

  useEffect(() => {
    let cancelled = false;
    if (!deviceType || !selectedBrandObj) {
      setModels([]);
      return;
    }

    setLoadingModels(true);
    deviceService
      .getPublicModels({ deviceType, brandId: selectedBrandObj.id, pageSize: 100 })
      .then((res) => {
        if (!cancelled) {
          setModels(res.items ?? []);
        }
      })
      .catch(() => {
        if (!cancelled) setModels([]);
      })
      .finally(() => {
        if (!cancelled) setLoadingModels(false);
      });

    return () => {
      cancelled = true;
    };
  }, [deviceType, selectedBrandObj?.id]);

  return {
    brands,
    models,
    selectedBrandObj,
    loadingBrands,
    loadingModels,
  };
}
