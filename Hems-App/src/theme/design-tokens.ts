/**
 * Design Tokens - Single Source of Truth for All Colors
 *
 * Bu dosya tüm uygulamadaki renkleri kontrol eder.
 * Herhangi bir rengi değiştirmek için sadece bu dosyayı düzenleyin.
 */

// ============================================================================
// LIGHT MODE DESIGN TOKENS
// ============================================================================

export const DESIGN_TOKENS_LIGHT = {
  // === BRAND COLORS (Marka renkleri) ===
  brand: {
    primary: '#1169b1',           // Ana marka rengi (cyan)
    primaryLight: '#67e8f9',      // Açık varyant
    primaryDark: '#0891b2',       // Koyu varyant
    secondary: '#8b5cf6',         // İkincil marka rengi (purple)
    secondaryLight: '#a78bfa',    // Açık varyant
    secondaryDark: '#7c3aed',     // Koyu varyant
    accent: '#1169b1',            // Vurgu rengi
    accentGlow: '#1169b1',        // Glow efekti
  },

  // === SEMANTIC COLORS (Anlamsal renkler) ===
  semantic: {
    success: '#10b981',           // Başarı (yeşil)
    successLight: '#34d399',
    successDark: '#059669',
    error: '#ef4444',             // Hata (kırmızı)
    errorLight: '#f87171',
    errorDark: '#dc2626',
    warning: '#f59e0b',           // Uyarı (turuncu)
    warningLight: '#fbbf24',
    warningDark: '#d97706',
    info: '#3b82f6',              // Bilgi (mavi)
    infoLight: '#60a5fa',
    infoDark: '#2563eb',
  },

  // === TEXT COLORS (Metin renkleri) ===
  text: {
    primary: '#1b1b1bff',           // Ana metin (çok koyu gri)
    secondary: '#374151',         // İkincil metin (koyu gri)
    tertiary: '#6b7280',          // Üçüncül metin (orta gri)
    quaternary: '#9ca3af',        // Dördüncül metin (açık gri)
    disabled: '#d1d5db',          // Devre dışı
    inverse: '#ffffff',           // Ters renk (dark background üzerinde)
    link: '#1169b1',              // Link
    linkHover: '#0891b2',         // Link hover
    linkActive: '#0e7490',        // Link active
    placeholder: '#9ca3af',       // Placeholder text
    hint: '#6b7280',              // Hint text
  },

  // === BACKGROUND COLORS (Arka plan renkleri) ===
  background: {
    primary: '#ffffff',           // Ana arka plan
    secondary: '#f9fafb',         // İkincil arka plan
    tertiary: '#f3f4f6',          // Üçüncül arka plan
    quaternary: '#e5e7eb',        // Dördüncül arka plan
    elevated: '#ffffff',          // Yükseltilmiş (cards, modals)
    overlay: 'rgba(0, 0, 0, 0.5)', // Modal overlay
    page: '#f5f7fb',              // Sayfa arka planı
    shade: '#f1f5f9',             // Hafif gölge
    shadeDark: '#e2e8f0',         // Koyu gölge
    hover: '#f9fafb',             // Hover state
    pressed: '#f3f4f6',           // Pressed state
    selected: '#e0f2fe',          // Selected state
  },

  // === BORDER COLORS (Border renkleri) ===
  border: {
    primary: '#e5e7eb',           // Ana border
    secondary: '#d1d5db',         // İkincil border
    tertiary: '#9ca3af',          // Üçüncül border
    light: '#f3f4f6',             // Hafif border
    focus: '#1169b1',             // Focus state
    error: '#ef4444',             // Hata border
    success: '#10b981',           // Başarı border
    warning: '#f59e0b',           // Uyarı border
    disabled: '#f3f4f6',          // Devre dışı
    divider: '#e5e7eb',           // Ayırıcı çizgi
  },

  // === SURFACE COLORS (Yüzey renkleri - kartlar, panels) ===
  surface: {
    base: '#ffffff',              // Temel yüzey
    elevated: '#ffffff',          // Yükseltilmiş yüzey
    overlay: '#f9fafb',           // Overlay yüzey
    card: '#ffffff',              // Kart arka planı
    cardHover: '#f9fafb',         // Kart hover
    panel: '#ffffff',             // Panel arka planı
    modal: '#ffffff',             // Modal arka planı
    dropdown: '#ffffff',          // Dropdown arka planı
    tooltip: '#1f2937',           // Tooltip arka planı
  },

  // === BUTTON COLORS ===
  button: {
    primaryBg: '#1169b1',
    primaryText: '#ffffff',
    primaryHover: '#0891b2',
    primaryPressed: '#0e7490',
    primaryDisabled: '#d1d5db',

    secondaryBg: '#f9fafb',
    secondaryText: '#374151',
    secondaryBorder: '#e5e7eb',
    secondaryHover: '#f3f4f6',
    secondaryPressed: '#e5e7eb',

    dangerBg: '#ef4444',
    dangerText: '#ffffff',
    dangerHover: '#dc2626',
    dangerPressed: '#b91c1c',

    ghostBg: 'transparent',
    ghostText: '#374151',
    ghostHover: '#f9fafb',

    disabledBg: '#f3f4f6',
    disabledText: '#9ca3af',
    disabledBorder: '#e5e7eb',
  },

  // === INPUT COLORS ===
  input: {
    bg: '#ffffff',
    bgDisabled: '#f9fafb',
    border: '#e5e7eb',
    borderHover: '#d1d5db',
    borderFocus: '#1169b1',
    borderError: '#ef4444',
    borderSuccess: '#10b981',
    text: '#111827',
    textDisabled: '#9ca3af',
    placeholder: '#9ca3af',
    label: '#374151',
    hint: '#6b7280',
    error: '#ef4444',
    success: '#10b981',
  },

  // === CHART COLORS (Grafik renkleri) ===
  chart: {
    primary: '#1169b1',
    secondary: '#8b5cf6',
    tertiary: '#f59e0b',
    quaternary: '#10b981',
    quinary: '#ef4444',
    senary: '#ec4899',

    line1: '#1169b1',
    line2: '#8b5cf6',
    line3: '#f59e0b',
    line4: '#10b981',
    line5: '#ef4444',

    area1: 'rgba(6, 182, 212, 0.1)',
    area2: 'rgba(139, 92, 246, 0.1)',
    area3: 'rgba(245, 158, 11, 0.1)',

    grid: '#e5e7eb',
    gridLight: '#f3f4f6',
    axis: '#6b7280',
    label: '#374151',
    tooltip: '#1f2937',
    tooltipText: '#ffffff',
  },

  // === ENERGY COLORS (Enerji sistemi renkleri) ===
  energy: {
    solar: '#fbbf24',             // Güneş (sarı/turuncu)
    solarLight: '#fcd34d',
    solarDark: '#f59e0b',

    battery: '#10b981',           // Batarya (yeşil)
    batteryLight: '#34d399',
    batteryDark: '#059669',
    batteryCharging: '#10b981',
    batteryDischarging: '#f59e0b',

    grid: '#1169b1',              // Şebeke (cyan/mavi)
    gridLight: '#22d3ee',
    gridDark: '#0891b2',
    gridImport: '#ef4444',        // Şebekeden çekme (kırmızı)
    gridExport: '#10b981',        // Şebekeye verme (yeşil)

    load: '#8b5cf6',              // Yük/Tüketim (mor)
    loadLight: '#a78bfa',
    loadDark: '#7c3aed',

    generator: '#f97316',         // Jeneratör (turuncu)
    generatorLight: '#fb923c',
    generatorDark: '#ea580c',
  },

  // === STATUS COLORS (Durum renkleri) ===
  status: {
    online: '#10b981',            // Çevrimiçi
    offline: '#ef4444',           // Çevrimdışı
    idle: '#f59e0b',              // Boşta
    active: '#1169b1',            // Aktif
    pending: '#f59e0b',           // Beklemede
    completed: '#10b981',         // Tamamlandı
    failed: '#ef4444',            // Başarısız
    warning: '#f59e0b',           // Uyarı
  },

  // === BADGE COLORS ===
  badge: {
    defaultBg: '#f3f4f6',
    defaultText: '#374151',
    defaultBorder: '#e5e7eb',

    primaryBg: '#dbeafe',
    primaryText: '#1e40af',
    primaryBorder: '#93c5fd',

    successBg: '#d1fae5',
    successText: '#065f46',
    successBorder: '#6ee7b7',

    errorBg: '#fee2e2',
    errorText: '#991b1b',
    errorBorder: '#fca5a5',

    warningBg: '#fef3c7',
    warningText: '#92400e',
    warningBorder: '#fcd34d',

    infoBg: '#dbeafe',
    infoText: '#1e40af',
    infoBorder: '#93c5fd',
  },

  // === SHADOW COLORS ===
  shadow: {
    xs: 'rgba(0, 0, 0, 0.05)',
    sm: 'rgba(0, 0, 0, 0.08)',
    md: 'rgba(0, 0, 0, 0.12)',
    lg: 'rgba(0, 0, 0, 0.15)',
    xl: 'rgba(0, 0, 0, 0.18)',
    xxl: 'rgba(0, 0, 0, 0.25)',
  },

  // === GRADIENT COLORS ===
  gradient: {
    primary: ['#1169b1', '#0891b2'],
    secondary: ['#8b5cf6', '#7c3aed'],
    success: ['#10b981', '#059669'],
    error: ['#ef4444', '#dc2626'],
    warning: ['#f59e0b', '#d97706'],
    energySolar: ['#fbbf24', '#f59e0b'],
    energyBattery: ['#10b981', '#059669'],
  },

  // === UTILITY COLORS ===
  utility: {
    white: '#ffffff',
    black: '#000000',
    transparent: 'transparent',

    // Gray scale
    gray50: '#f9fafb',
    gray100: '#f3f4f6',
    gray200: '#e5e7eb',
    gray300: '#d1d5db',
    gray400: '#9ca3af',
    gray500: '#6b7280',
    gray600: '#4b5563',
    gray700: '#374151',
    gray800: '#1f2937',
    gray900: '#111827',

    // Slate scale (alternatif gri)
    slate50: '#f8fafc',
    slate100: '#f1f5f9',
    slate200: '#e2e8f0',
    slate300: '#cbd5e1',
    slate400: '#94a3b8',
    slate500: '#64748b',
    slate600: '#475569',
    slate700: '#334155',
    slate800: '#1e293b',
    slate900: '#0f172a',
  },
};

// ============================================================================
// DARK MODE DESIGN TOKENS
// ============================================================================

export const DESIGN_TOKENS_DARK = {
  // === BRAND COLORS (Marka renkleri) ===
  brand: {
    primary: '#1169b1',           // Ana marka rengi (turkuaz)
    primaryLight: '#5ce1d3',      // Açık varyant
    primaryDark: '#1ab5a6',       // Koyu varyant
    secondary: '#a78bfa',         // İkincil marka rengi (açık mor)
    secondaryLight: '#c4b5fd',    // Açık varyant
    secondaryDark: '#8b5cf6',     // Koyu varyant
    accent: '#1169b1',            // Vurgu rengi
    accentGlow: '#1169b1',        // Glow efekti
  },

  // === SEMANTIC COLORS (Anlamsal renkler) ===
  semantic: {
    success: '#10b981',
    successLight: '#34d399',
    successDark: '#059669',
    error: '#ef4444',
    errorLight: '#f87171',
    errorDark: '#dc2626',
    warning: '#f59e0b',
    warningLight: '#fbbf24',
    warningDark: '#d97706',
    info: '#3b82f6',
    infoLight: '#60a5fa',
    infoDark: '#2563eb',
  },

  // === TEXT COLORS (Metin renkleri) ===
  text: {
    primary: '#ffffff',           // Ana metin
    secondary: '#cbd5e1',         // İkincil metin
    tertiary: '#94a3b8',          // Üçüncül metin
    quaternary: '#64748b',        // Dördüncül metin
    disabled: '#475569',          // Devre dışı
    inverse: '#111827',           // Ters renk
    link: '#2bd5c4',              // Link
    linkHover: '#5ce1d3',         // Link hover
    linkActive: '#1ab5a6',        // Link active
    placeholder: '#64748b',       // Placeholder text
    hint: '#94a3b8',              // Hint text
  },

  // === BACKGROUND COLORS (Arka plan renkleri) ===
  background: {
    primary: '#15191dff',           // Ana arka plan
    secondary: '#232b36',         // İkincil arka plan
    tertiary: '#2d3748',          // Üçüncül arka plan
    quaternary: '#374151',        // Dördüncül arka plan
    elevated: '#232b36',          // Yükseltilmiş
    overlay: 'rgba(0, 0, 0, 0.7)', // Modal overlay
    page: '#1b222a',              // Sayfa arka planı
    shade: '#2d343fff',             // Hafif gölge
    shadeDark: '#1a202c',         // Koyu gölge
    hover: '#2d3748',             // Hover state
    pressed: '#374151',           // Pressed state
    selected: '#272b3dff',          // Selected state
  },

  // === BORDER COLORS (Border renkleri) ===
  border: {
    primary: '#374151',           // Ana border
    secondary: '#4b5563',         // İkincil border
    tertiary: '#6b7280',          // Üçüncül border
    light: '#2d3748',             // Hafif border
    focus: '#2bd5c4',             // Focus state
    error: '#ef4444',             // Hata border
    success: '#10b981',           // Başarı border
    warning: '#f59e0b',           // Uyarı border
    disabled: '#2d3748',          // Devre dışı
    divider: '#374151',           // Ayırıcı çizgi
  },

  // === SURFACE COLORS (Yüzey renkleri) ===
  surface: {
    base: 'rgba(32, 40, 53, 1)',              // Temel yüzey
    elevated: '#232b36',          // Yükseltilmiş yüzey
    overlay: '#2d3748',           // Overlay yüzey
    card: '#232b36',              // Kart arka planı
    cardHover: '#2d3748',         // Kart hover
    panel: '#232b36',             // Panel arka planı
    modal: '#1f2937',             // Modal arka planı
    dropdown: '#1f2937',          // Dropdown arka planı
    tooltip: '#374151',           // Tooltip arka planı
  },

  // === BUTTON COLORS ===
  button: {
    primaryBg: '#2bd5c4',
    primaryText: '#ffffff',
    primaryHover: '#5ce1d3',
    primaryPressed: '#1ab5a6',
    primaryDisabled: '#475569',

    secondaryBg: '#374151',
    secondaryText: '#cbd5e1',
    secondaryBorder: '#4b5563',
    secondaryHover: '#4b5563',
    secondaryPressed: '#6b7280',

    dangerBg: '#ef4444',
    dangerText: '#ffffff',
    dangerHover: '#f87171',
    dangerPressed: '#dc2626',

    ghostBg: 'transparent',
    ghostText: '#cbd5e1',
    ghostHover: '#2d3748',

    disabledBg: '#2d3748',
    disabledText: '#64748b',
    disabledBorder: '#374151',
  },

  // === INPUT COLORS ===
  input: {
    bg: '#232b36',
    bgDisabled: '#1b222a',
    border: '#374151',
    borderHover: '#4b5563',
    borderFocus: '#2bd5c4',
    borderError: '#ef4444',
    borderSuccess: '#10b981',
    text: '#ffffff',
    textDisabled: '#64748b',
    placeholder: '#64748b',
    label: '#cbd5e1',
    hint: '#94a3b8',
    error: '#ef4444',
    success: '#10b981',
  },

  // === CHART COLORS (Grafik renkleri) ===
  chart: {
    primary: '#2bd5c4',
    secondary: '#a78bfa',
    tertiary: '#fbbf24',
    quaternary: '#34d399',
    quinary: '#f87171',
    senary: '#f472b6',

    line1: '#2bd5c4',
    line2: '#a78bfa',
    line3: '#fbbf24',
    line4: '#34d399',
    line5: '#f87171',

    area1: 'rgba(43, 213, 196, 0.15)',
    area2: 'rgba(167, 139, 250, 0.15)',
    area3: 'rgba(251, 191, 36, 0.15)',

    grid: '#374151',
    gridLight: '#2d3748',
    axis: '#94a3b8',
    label: '#cbd5e1',
    tooltip: '#374151',
    tooltipText: '#ffffff',
  },

  // === ENERGY COLORS (Enerji sistemi renkleri) ===
  energy: {
    solar: '#fbbf24',
    solarLight: '#fcd34d',
    solarDark: '#f59e0b',

    battery: '#10b981',
    batteryLight: '#34d399',
    batteryDark: '#059669',
    batteryCharging: '#10b981',
    batteryDischarging: '#f59e0b',

    grid: '#2bd5c4',
    gridLight: '#5ce1d3',
    gridDark: '#1ab5a6',
    gridImport: '#ef4444',
    gridExport: '#10b981',

    load: '#a78bfa',
    loadLight: '#c4b5fd',
    loadDark: '#8b5cf6',

    generator: '#f97316',
    generatorLight: '#fb923c',
    generatorDark: '#ea580c',
  },

  // === STATUS COLORS (Durum renkleri) ===
  status: {
    online: '#10b981',
    offline: '#ef4444',
    idle: '#f59e0b',
    active: '#2bd5c4',
    pending: '#f59e0b',
    completed: '#10b981',
    failed: '#ef4444',
    warning: '#f59e0b',
  },

  // === BADGE COLORS ===
  badge: {
    defaultBg: '#2d3748',
    defaultText: '#cbd5e1',
    defaultBorder: '#374151',

    primaryBg: '#164e63',
    primaryText: '#67e8f9',
    primaryBorder: '#0891b2',

    successBg: '#064e3b',
    successText: '#6ee7b7',
    successBorder: '#059669',

    errorBg: '#7f1d1d',
    errorText: '#fca5a5',
    errorBorder: '#dc2626',

    warningBg: '#78350f',
    warningText: '#fcd34d',
    warningBorder: '#d97706',

    infoBg: '#1e3a8a',
    infoText: '#93c5fd',
    infoBorder: '#2563eb',
  },

  // === SHADOW COLORS ===
  shadow: {
    xs: 'rgba(0, 0, 0, 0.15)',
    sm: 'rgba(0, 0, 0, 0.18)',
    md: 'rgba(0, 0, 0, 0.22)',
    lg: 'rgba(0, 0, 0, 0.25)',
    xl: 'rgba(0, 0, 0, 0.3)',
    xxl: 'rgba(0, 0, 0, 0.4)',
  },

  // === GRADIENT COLORS ===
  gradient: {
    primary: ['#2bd5c4', '#1ab5a6'],
    secondary: ['#a78bfa', '#8b5cf6'],
    success: ['#34d399', '#059669'],
    error: ['#f87171', '#dc2626'],
    warning: ['#fbbf24', '#d97706'],
    energySolar: ['#fbbf24', '#f59e0b'],
    energyBattery: ['#34d399', '#059669'],
  },

  // === UTILITY COLORS ===
  utility: {
    white: '#ffffff',
    black: '#000000',
    transparent: 'transparent',

    // Gray scale
    gray50: '#f9fafb',
    gray100: '#f3f4f6',
    gray200: '#e5e7eb',
    gray300: '#d1d5db',
    gray400: '#9ca3af',
    gray500: '#6b7280',
    gray600: '#4b5563',
    gray700: '#374151',
    gray800: '#1f2937',
    gray900: '#111827',

    // Slate scale
    slate50: '#f8fafc',
    slate100: '#f1f5f9',
    slate200: '#e2e8f0',
    slate300: '#cbd5e1',
    slate400: '#94a3b8',
    slate500: '#64748b',
    slate600: '#475569',
    slate700: '#334155',
    slate800: '#1e293b',
    slate900: '#0f172a',
  },
};

// ============================================================================
// HELPER FUNCTIONS
// ============================================================================

/**
 * Get design tokens based on theme mode
 */
export function getDesignTokens(isDark: boolean) {
  return isDark ? DESIGN_TOKENS_DARK : DESIGN_TOKENS_LIGHT;
}

/**
 * Type definition for design tokens
 */
export type DesignTokens = typeof DESIGN_TOKENS_LIGHT;

/**
 * Font weight to font family mapping for Montserrat
 * Use these to apply different font weights
 */
export const getFontFamily = (weight: '300' | '400' | '500' | '600' | '700' = '400') => {
  const fontMap = {
    '300': 'Montserrat-Light',
    '400': 'Montserrat-Regular',
    '500': 'Montserrat-Medium',
    '600': 'Montserrat-SemiBold',
    '700': 'Montserrat-Bold',
  };
  return fontMap[weight];
};
