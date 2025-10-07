import { StyleSheet } from 'react-native';

export const makeFaultsScreenStyles = (COLORS: Record<string, string>, isDark: boolean) =>
    StyleSheet.create({
        screen: {
            flex: 1,
            backgroundColor: isDark ? COLORS.dark : COLORS.white,
        },
        headerRow: {
            flexDirection: 'row',
            alignItems: 'center',
            justifyContent: 'space-between',
            paddingHorizontal: 16,
            marginBottom: 12,
        },
        backButton: {
            padding: 8,
        },
        screenTitle: {
            fontSize: 20,
            fontWeight: '700',
            color: isDark ? COLORS.white : COLORS.black,
            textAlign: 'center',
            flex: 1,
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
            paddingHorizontal: 16,
            paddingBottom: 24,
        },
        separator: {
            height: 12,
        },
        itemPress: {
            borderRadius: 12,
            overflow: 'hidden',
        },
        itemRow: {
            flexDirection: 'row',
            borderLeftWidth: 4,
            borderRadius: 12,
            backgroundColor: isDark ? COLORS.surface : COLORS.white,
            shadowColor: COLORS.black,
            shadowOpacity: 0.08,
            shadowOffset: { width: 0, height: 2 },
            shadowRadius: 6,
            elevation: 2,
            padding: 14,
        },
        itemContent: {
            flex: 1,
        },
        itemHeader: {
            flexDirection: 'row',
            justifyContent: 'space-between',
            alignItems: 'center',
            marginBottom: 6,
        },
        itemTitle: {
            fontSize: 17,
            fontWeight: '600',
            color: isDark ? COLORS.white : COLORS.black,
        },
        itemMsg: {
            fontSize: 14,
            color: COLORS.textSoft,
            marginTop: 2,
        },
        itemMetaRow: {
            marginTop: 8,
            flexDirection: 'row',
            justifyContent: 'flex-end',
        },
        itemMeta: {
            fontSize: 12,
            color: COLORS.slate400,
        },

        statusBadge: {
            paddingHorizontal: 10,
            paddingVertical: 4,
            borderRadius: 10,
        },
        statusBadgeText: {
            color: '#fff',
            fontSize: 12,
            fontWeight: '600',
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
            justifyContent: 'space-around',
            marginHorizontal: 16,
            marginTop: 8,
            marginBottom: 12,
        },
        filterChip: {
            paddingHorizontal: 12,
            paddingVertical: 6,
            borderRadius: 16,
            backgroundColor: 'transparent',
            borderWidth: 1,
            borderColor: COLORS.border,
        },
        filterChipActive: {
            backgroundColor: COLORS.accent,
            borderColor: COLORS.accent,
        },
        filterText: {
            fontSize: 14,
            color: COLORS.textSoft,
        },
        filterTextActive: {
            color: COLORS.white,
            fontWeight: '600',
        },
    });
