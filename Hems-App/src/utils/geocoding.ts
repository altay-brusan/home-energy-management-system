export interface GeocodingResult {
  country: string | null;
  city: string | null;
  district: string | null;
  displayName: string | null;
}

export async function reverseGeocode(
  latitude: number,
  longitude: number
): Promise<GeocodingResult> {
  try {
    const response = await fetch(
      `https://nominatim.openstreetmap.org/reverse?lat=${latitude}&lon=${longitude}&format=json&accept-language=en`,
      {
        headers: {
          'User-Agent': 'NetzrApp/1.0',
        },
      }
    );

    if (!response.ok) {
      throw new Error('Geocoding request failed');
    }

    const data = await response.json();
    const address = data.address || {};

    return {
      country: address.country || null,
      city: address.city || address.town || address.municipality || address.village || address.state || null,
      district: address.suburb || address.neighbourhood || address.district || address.county || null,
      displayName: data.display_name || null,
    };
  } catch (error) {
    console.warn('[Geocoding] Reverse geocoding failed:', error);
    return {
      country: null,
      city: null,
      district: null,
      displayName: null,
    };
  }
}
