import { StyleSheet } from 'react-native';
import { SPACING } from '../../../../theme/constants';

export const makeChangePasswordStyles = (COLORS: Record<string, string>) => StyleSheet.create({
    container: {
        flex: 1,
        padding: SPACING.xl,
        justifyContent: 'flex-start',
        backgroundColor: COLORS.dark,
    },
    avatarWrap: {
        alignItems: 'center',
        marginBottom: SPACING.lg,
    },
    avatar: {
        width: 80,
        height: 80,
        borderRadius: 40,
        borderWidth: 2,
        borderColor: COLORS.accent,
        marginBottom: 2,
    },
    title: {
        fontSize: 22,
        fontWeight: '700',
        marginBottom: SPACING.lg,
        textAlign: 'center',
        color: COLORS.accent,
    },
    messageBox: {
        marginBottom: SPACING.sm,
        marginTop: 2,
        padding: SPACING.md,
        borderRadius: 8,
        backgroundColor: COLORS.bgShadeDark,
        alignItems: 'center',
    },
    messageText: {
        fontSize: 15,
        fontWeight: '600',
    },
    errorText: {
        color: COLORS.error,
    },
    successText: {
        color: COLORS.success,
    },
    buttonRow: {
        flexDirection: 'row',
        justifyContent: 'space-between',
        marginTop: SPACING.md,
        gap: 10,
    },
    saveBtn: {
        flex: 1,
        backgroundColor: COLORS.accent,
        borderRadius: 8,
        alignItems: 'center',
        paddingVertical: SPACING.md,
        marginRight: 4,
    },
    saveBtnText: {
        color: COLORS.white,
        fontWeight: '700',
        fontSize: 16,
    },
    cancelBtn: {
        flex: 1,
        backgroundColor: COLORS.bgShadeDark,
        borderRadius: 8,
        alignItems: 'center',
        paddingVertical: SPACING.md,
        marginLeft: 4,
    },
    cancelBtnText: {
        color: COLORS.textSoft,
        fontWeight: '700',
        fontSize: 16,
    },
    avatarTouchable: {
        position: 'relative',
        alignItems: 'center',
        justifyContent: 'center',
    },
    avatarEditBadge: {
        position: 'absolute',
        right: 0,
        bottom: 0,
        backgroundColor: COLORS.accent,
        borderRadius: 12,
        paddingHorizontal: 6,
        paddingVertical: 2,
        borderWidth: 2,
        borderColor: COLORS.white,
        alignItems: 'center',
        justifyContent: 'center',
    },
    inputGroup: {
        width: '100%',
        gap: 12,
        marginBottom: 18,
    },
    passwordRules: {
        marginTop: 8,
        marginBottom: 16,
        padding: 8,
        backgroundColor: COLORS.bgShadeLight,
        borderRadius: 8,
    },
    passwordRulesTitle: {
        fontSize: 14,
        fontWeight: '600',
        color: COLORS.textSoft,
        marginBottom: 4,
    },
    passwordRule: {
        fontSize: 12,
        color: COLORS.textSoft,
        marginBottom: 2,
    },
});