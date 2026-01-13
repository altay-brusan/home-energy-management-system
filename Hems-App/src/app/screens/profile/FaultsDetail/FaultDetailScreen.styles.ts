import { StyleSheet } from 'react-native';

export const makeFaultDetailStyles = (COLORS: Record<string, string>, isDark: boolean) =>
    StyleSheet.create({
        screen: {
            flex: 1,
            backgroundColor: isDark ? COLORS.dark : '#F3F4F6',
        },
        container: {
            flex: 1,
        },

        headerRow: {
            flexDirection: 'row',
            alignItems: 'center',
            justifyContent: 'space-between',
            paddingHorizontal: 16,
            paddingBottom: 12,
            borderBottomWidth: 1,
            borderBottomColor: COLORS.border,
        },
        backButton: {
            width: 40,
            height: 40,
            borderRadius: 20,
            alignItems: 'center',
            justifyContent: 'center',
        },
        moreButton: {
            width: 40,
            height: 40,
            borderRadius: 20,
            alignItems: 'center',
            justifyContent: 'center',
        },
        screenTitle: {
            fontSize: 20,
            fontWeight: '700',
            textAlign: 'center',
            flex: 1,
        },

        statusBadge: {
            position: 'absolute',
            top: 12,
            left: 12,
            flexDirection: 'row',
            alignItems: 'center',
            paddingHorizontal: 10,
            paddingVertical: 4,
            borderRadius: 10,
            gap: 6,
        },
        statusText: {
            fontSize: 14,
            fontWeight: '600',
        },

        heroCard: {
            backgroundColor: isDark ? COLORS.surface : COLORS.white,
            marginHorizontal: 16,
            marginTop: 16,
            padding: 24,
            borderRadius: 20,
            alignItems: 'center',
            shadowColor: '#000',
            shadowOffset: { width: 0, height: 2 },
            shadowOpacity: 0.08,
            shadowRadius: 12,
            elevation: 3,
            position: 'relative',
        },
        iconCircle: {
            width: 72,
            height: 72,
            borderRadius: 36,
            alignItems: 'center',
            justifyContent: 'center',
            marginBottom: 16,
        },
        iconCircleSm: {
            width: 28,
            height: 28,
            borderRadius: 14,
            alignItems: 'center',
            justifyContent: 'center',
        },
        faultType: {
            fontSize: 24,
            fontWeight: '700',
            color: isDark ? COLORS.white : COLORS.black,
            marginBottom: 8,
            textAlign: 'center',
        },
        description: {
            fontSize: 16,
            color: COLORS.textSoft,
            textAlign: 'center',
            lineHeight: 22,
        },

        section: {
            marginTop: 24,
            marginHorizontal: 16,
        },
        sectionTitle: {
            fontSize: 17,
            fontWeight: '700',
            color: isDark ? COLORS.white : COLORS.black,
            marginBottom: 12,
            marginLeft: 4,
        },
        sectionCard: {
            backgroundColor: isDark ? COLORS.surface : COLORS.white,
            padding: 20,
            borderRadius: 16,
            shadowColor: '#000',
            shadowOffset: { width: 0, height: 1 },
            shadowOpacity: 0.05,
            shadowRadius: 8,
            elevation: 2,
        },

        noteHeader: {
            flexDirection: 'row',
            alignItems: 'center',
        },
        noteCard: {
            marginTop: 8,
            backgroundColor: isDark ? COLORS.surface : COLORS.white,
            borderRadius: 12,
            padding: 16,
            shadowColor: '#000',
            shadowOpacity: 0.08,
            shadowRadius: 6,
            elevation: 2,
        },
        noteItem: {
            marginBottom: 12,
        },
        noteItemExpanded: {
            paddingVertical: 8,
        },
        notePressable: {
            flexDirection: 'row',
            alignItems: 'center',
            flex: 1,
            paddingVertical: 6,
        },
        noteTextClickable: {
            fontSize: 17,
            fontWeight: '600',
            color: COLORS.accent,
            marginLeft: 8,
        },

        requestContainer: {
            marginTop: 12,
            marginLeft: 28,
            borderWidth: 1,
            borderColor: COLORS.border,
            borderRadius: 10,
            padding: 12,
            backgroundColor: isDark ? COLORS.surface : COLORS.white,
        },
        requestCard: {
            backgroundColor: isDark ? COLORS.dark : '#F9FAFB',
            borderRadius: 10,
            padding: 12,
            marginTop: 8,
        },
        requestText: {
            fontSize: 15,
            color: COLORS.textSoft,
        },

        newNoteRow: {
            flexDirection: 'row',
            alignItems: 'center',
            marginTop: 12,
            gap: 8,
        },
        noteInputBig: {
            flex: 1,
            borderWidth: 1,
            borderColor: COLORS.border,
            borderRadius: 10,
            paddingHorizontal: 14,
            paddingVertical: 12,
            fontSize: 16,
            color: isDark ? COLORS.white : COLORS.black,
            backgroundColor: isDark ? COLORS.dark : '#F9FAFB',
        },
        addBtnBig: {
            backgroundColor: COLORS.accent,
            borderRadius: 10,
            padding: 12,
            alignItems: 'center',
            justifyContent: 'center',
            marginLeft: 8,
        },

        actionSection: {
            marginTop: 32,
            marginHorizontal: 16,
            gap: 12,
        },
        primaryBtn: {
            backgroundColor: COLORS.accent,
            borderRadius: 16,
            paddingVertical: 16,
            alignItems: 'center',
            justifyContent: 'center',
            shadowColor: COLORS.accent,
            shadowOffset: { width: 0, height: 4 },
            shadowOpacity: 0.3,
            shadowRadius: 8,
            elevation: 4,
        },
        primaryBtnText: {
            color: COLORS.white,
            fontWeight: '700',
            fontSize: 16,
        },
        secondaryBtn: {
            backgroundColor: isDark ? COLORS.surface : COLORS.white,
            borderRadius: 16,
            paddingVertical: 16,
            alignItems: 'center',
            justifyContent: 'center',
            borderWidth: 2,
            borderColor: COLORS.accent,
        },
        secondaryBtnText: {
            color: COLORS.accent,
            fontWeight: '700',
            fontSize: 16,
        },

        // InfoRow
        infoRow: {
            marginBottom: 16,
        },
        infoRowLast: {
            marginBottom: 0,
        },
        infoLabelContainer: {
            flexDirection: 'row',
            alignItems: 'center',
            marginBottom: 6,
            gap: 6,
        },
        infoLabel: {
            fontSize: 13,
            color: COLORS.textSoft,
            fontWeight: '600',
        },
        infoValue: {
            fontSize: 16,
            color: isDark ? COLORS.white : COLORS.black,
            fontWeight: '500',
            paddingLeft: 32,
        },

        timelineItem: {
            flexDirection: 'row',
            paddingBottom: 16,
        },
        timelineItemLast: {
            paddingBottom: 0,
        },
        timelineIconContainer: {
            alignItems: 'center',
            marginRight: 12,
        },
        timelineCircle: {
            width: 36,
            height: 36,
            borderRadius: 18,
            alignItems: 'center',
            justifyContent: 'center',
        },
        timelineLine: {
            width: 2,
            flex: 1,
            marginTop: 4,
        },
        timelineContent: {
            flex: 1,
            paddingTop: 4,
        },
        timelineLabel: {
            fontSize: 14,
            color: COLORS.textSoft,
            fontWeight: '600',
            marginBottom: 2,
        },
        timelineValue: {
            fontSize: 15,
            color: isDark ? COLORS.white : COLORS.black,
            fontWeight: '500',
        },
    });
