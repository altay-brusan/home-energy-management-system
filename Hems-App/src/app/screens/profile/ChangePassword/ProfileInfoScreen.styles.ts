import { StyleSheet } from 'react-native';
import { SPACING } from '../../../../theme/constants';

export const makeChangePasswordStyles = (COLORS: Record<string, string>, isDark: boolean = true) => StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: isDark ? COLORS.dark : COLORS.gray100,
  },
  header: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    paddingHorizontal: 16,
    paddingVertical: 12,
  },
  backButton: {
    padding: 8,
  },
  headerTitle: {
    fontSize: 18,
    fontWeight: '600',
    flex: 1,
    textAlign: 'center',
    marginHorizontal: 8,
    color: COLORS.text,
  },
  scrollContent: {
    paddingHorizontal: 16,
    paddingTop: 16,
  },
  section: {
    backgroundColor: isDark ? COLORS.surface : COLORS.white,
    borderRadius: 14,
    padding: SPACING.md,
    marginBottom: SPACING.md,
  },
  sectionTitle: {
    fontSize: 13,
    fontWeight: '600',
    color: COLORS.textSoft,
    marginBottom: SPACING.md,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
  },
  inputRow: {
    marginBottom: SPACING.sm,
  },
  inputLabel: {
    fontSize: 13,
    fontWeight: '500',
    color: COLORS.textSoft,
    marginBottom: 6,
  },
  inputField: {
    backgroundColor: isDark ? 'rgba(255,255,255,0.05)' : 'rgba(0,0,0,0.03)',
    borderRadius: 10,
    paddingHorizontal: 14,
    paddingVertical: 12,
    fontSize: 15,
    color: COLORS.text,
    borderWidth: 1,
    borderColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.06)',
  },
  passwordRulesCard: {
    backgroundColor: isDark ? COLORS.surface : COLORS.white,
    borderRadius: 14,
    padding: SPACING.md,
    marginBottom: SPACING.md,
  },
  passwordRulesTitle: {
    fontSize: 13,
    fontWeight: '600',
    color: COLORS.textSoft,
    marginBottom: SPACING.sm,
    textTransform: 'uppercase',
    letterSpacing: 0.5,
  },
  passwordRule: {
    fontSize: 13,
    color: COLORS.textSoft,
    marginBottom: 4,
    paddingLeft: 4,
  },
  messageBox: {
    marginBottom: SPACING.md,
    padding: SPACING.md,
    borderRadius: 10,
    alignItems: 'center',
  },
  successBox: {
    backgroundColor: `${COLORS.success}15`,
  },
  errorBox: {
    backgroundColor: `${COLORS.error}15`,
  },
  messageText: {
    fontSize: 14,
    fontWeight: '600',
  },
  successText: {
    color: COLORS.success,
  },
  errorText: {
    color: COLORS.error,
  },
  buttonContainer: {
    paddingHorizontal: 16,
    paddingBottom: 16,
    gap: 12,
  },
  saveButton: {
    backgroundColor: COLORS.accent,
    borderRadius: 12,
    paddingVertical: 16,
    alignItems: 'center',
  },
  saveButtonText: {
    color: COLORS.white,
    fontSize: 16,
    fontWeight: '700',
  },
  cancelButton: {
    backgroundColor: isDark ? COLORS.surface : COLORS.white,
    borderRadius: 12,
    paddingVertical: 16,
    alignItems: 'center',
    borderWidth: 1,
    borderColor: isDark ? 'rgba(255,255,255,0.1)' : 'rgba(0,0,0,0.08)',
  },
  cancelButtonText: {
    color: COLORS.textSoft,
    fontSize: 16,
    fontWeight: '600',
  },
});
