import { StyleSheet } from 'react-native';

export const makeFaultsScreenStyles = (COLORS: Record<string, string>, isDark: boolean) => {
    const APP_BG = COLORS?.appBg ?? (isDark ? '#1c222b' : '#F2F4F7');
    const TEXT = COLORS?.text ?? (isDark ? '#E6EAF2' : '#111827');

    return StyleSheet.create({
        screen: {
            flex: 1,
            backgroundColor: APP_BG,
        },
        headerRow: {
            flexDirection: 'row',
            alignItems: 'center',
            justifyContent: 'space-between',
            marginBottom: 16,
            paddingHorizontal: 16,
            paddingVertical: 4,
        },
        backButton: {
            padding: 8,
        },
        screenTitle: {
            fontSize: 26,
            fontWeight: '700',
            color: TEXT,
            letterSpacing: -0.5,
        },
        warrantyCard: {
            marginHorizontal: 16,
            marginBottom: 12,
            padding: 14,
            borderRadius: 12,
            backgroundColor: isDark ? COLORS.surface : COLORS.surface,
            borderWidth: 1,
            borderColor: COLORS.border,
        },
        warrantyText: {
            fontSize: 14,
            fontWeight: '500',
            color: isDark ? COLORS.white : COLORS.black,
            textAlign: "center"
        },
        pageScroll: {
            flexGrow: 1,
            paddingHorizontal: 0,
            paddingBottom: 16,
        },
        separator: {
            height: 1,
            backgroundColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.08)',
            marginVertical: 0,
        },
        itemPress: {
            paddingVertical: 0,
            paddingHorizontal: 0,
        },
        itemRow: {
            flexDirection: 'row',
            alignItems: 'flex-start',
            paddingVertical: 18,
            paddingHorizontal: 16,
            borderLeftWidth: 0,
            backgroundColor: isDark ? 'rgba(32, 40, 53, 1)' : 'rgba(249,250,251,0.9)',
            borderRadius: 0,
            minHeight: 96,
        },
        itemContent: {
            flex: 1,
        },
        itemHeader: {
            flexDirection: 'row',
            justifyContent: 'space-between',
            alignItems: 'flex-start',
            marginBottom: 6,
        },
        itemTitle: {
            color: TEXT,
            fontSize: 16,
            fontWeight: '700',
            flex: 1,
            flexShrink: 1,
            marginRight: 10,
            lineHeight: 22,
            marginBottom: 4,
            letterSpacing: -0.3,
        },
        itemMsg: {
            color: COLORS.textMuted ?? (isDark ? 'rgba(230,234,242,0.64)' : '#6B7280'),
            fontSize: 14,
            marginBottom: 10,
            lineHeight: 20,
            fontWeight: '400',
            letterSpacing: -0.1,
        },
        itemMetaRow: {
            flexDirection: 'row',
            justifyContent: 'space-between',
            alignItems: 'center',
            marginTop: 6,
        },
        itemMeta: {
            color: COLORS.textMuted ?? (isDark ? 'rgba(230,234,242,0.64)' : '#6B7280'),
            fontSize: 12,
            textAlign: 'right',
            fontWeight: '500',
            letterSpacing: -0.1,
        },

        statusBadge: {
            paddingHorizontal: 14,
            paddingVertical: 6,
            borderRadius: 8,
            marginLeft: 10,
            alignSelf: 'center',
            flexShrink: 0,
        },
        statusBadgeText: {
            fontSize: 11,
            fontWeight: '700',
            color: '#FFFFFF',
            textTransform: 'capitalize',
            letterSpacing: 0.3,
        },
        createBtn: {
            marginHorizontal: 16,
            marginBottom: 12,
            backgroundColor: COLORS.accent,
            borderRadius: 12,
            paddingVertical: 14,
            alignItems: 'center',
            justifyContent: 'center',
        },
        createBtnText: {
            color: COLORS.white,
            fontWeight: '600',
            fontSize: 16,
        },
        sectionTitle: {
            fontSize: 24,
            fontWeight: '600',
            marginHorizontal: 16,
            marginVertical: 8,
            color: isDark ? COLORS.white : COLORS.black,
        },
        filterRow: {
            flexDirection: 'row',
            alignItems: 'center',
            gap: 6,
            paddingHorizontal: 16,
            paddingTop: 4,
            paddingBottom: 16,
            backgroundColor: APP_BG,
        },
        filterChip: {
            alignItems: 'center',
            justifyContent: 'center',
            paddingVertical: 10,
            paddingHorizontal: 18,
            borderRadius: 22,
            backgroundColor: isDark ? 'rgba(255,255,255,0.06)' : 'rgba(0,0,0,0.04)',
            borderWidth: 0,
            flexShrink: 0,
        },
        filterChipActive: {
            backgroundColor: COLORS.accent ?? '#1DD1A1',
            borderWidth: 0,
        },
        filterText: {
            color: isDark ? '#D1D5DB' : '#6B7280',
            fontWeight: '600',
            fontSize: 13,
            textAlign: 'center',
            includeFontPadding: false,
            letterSpacing: -0.2,
        },
        filterTextActive: {
            color: '#FFFFFF',
            fontWeight: '700',
            fontSize: 13,
            textAlign: 'center',
            includeFontPadding: false,
            letterSpacing: -0.2,
        },
    });
};
