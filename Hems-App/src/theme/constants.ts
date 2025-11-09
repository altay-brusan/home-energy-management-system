export const DARK_COLORS = {
  dark: '#1b222a',             // Koyu gri-mavi arka plan
  border: '#374151',           // Orta gri border
  textSoft: '#cbd5e1',         // Açık gri metin
  accent: '#2bd5c4',           // Turkuaz accent
  accentGlow: '#2bd5c4',       // Turkuaz glow efekti
  white: '#ffffff',            // Beyaz
  black: '#000000',            // Siyah
  surface: '#232b36',          // Koyu kart yüzeyi (Today Summary ile aynı)
  buttonBg: '#374151',         // Koyu gri buton arka planı
  bgShadeLight: '#374151',     // Açık gri gölge
  bgShadeDark: '#1a202c',      // Koyu gri gölge

  error: '#ef4444',            // Kırmızı hata rengi
  warning: '#f59e0b',          // Turuncu uyarı rengi
  success: '#10b981',          // Yeşil başarı rengi
  info: '#3b82f6',             // Mavi bilgi rengi
  disabled: '#6b7280',         // Gri devre dışı rengi
  slate900: '#111827',         // Çok koyu gri
  slate700: '#374151',         // Koyu gri
  slate400: '#9ca3af',         // Orta gri
  gray100: '#f3f4f6',          // Çok açık gri
  gray200: '#e5e7eb',          // Açık gri
  gray50: '#f9fafb',           // Neredeyse beyaz gri
  cardBg: '#232b36',           // Koyu kart arka planı
};

export const LIGHT_COLORS = {
  dark: '#f5f7fb',             // Açık mavi-gri arka plan
  border: '#e5e7eb',           // Açık gri border
  textSoft: '#374151',         // Koyu gri metin
  accent: '#06b6d4',           // Cyan accent
  accentGlow: '#06b6d4',       // Cyan glow efekti
  white: '#ffffff',            // Beyaz
  black: '#000000',            // Siyah
  surface: '#ffffff',          // Beyaz yüzey (kartlar)
  buttonBg: '#f9fafb',         // Çok açık gri buton arka planı
  bgShadeLight: '#f1f5f9',     // Açık mavi-gri gölge
  bgShadeDark: '#e2e8f0',      // Orta gri gölge

  error: '#ef4444',            // Kırmızı hata rengi
  warning: '#f59e0b',          // Turuncu uyarı rengi
  success: '#10b981',          // Yeşil başarı rengi
  info: '#3b82f6',             // Mavi bilgi rengi
  disabled: '#6b7280',         // Gri devre dışı rengi
  slate900: '#111827',         // Çok koyu gri
  slate700: '#374151',         // Koyu gri
  slate400: '#9ca3af',         // Orta gri
  gray100: '#f3f4f6',          // Çok açık gri
  gray200: '#e5e7eb',          // Açık gri
  gray50: '#f9fafb',           // Neredeyse beyaz gri
  cardBg: '#ffffff',           // Beyaz kart arka planı
};


export function getThemeColors (isDark: boolean) {
  return isDark ? DARK_COLORS : LIGHT_COLORS;
}

export const SPACING = {
  xs: 8,
  sm: 12,
  md: 16,
  lg: 24,
  xl: 32,
  xxl: 40,
};

export const TYPOGRAPHY = {
  xs: 12,
  sm: 14,
  md: 16,
  lg: 18,
  xl: 20,
  xxl: 24,
  xxxl: 32,
};

