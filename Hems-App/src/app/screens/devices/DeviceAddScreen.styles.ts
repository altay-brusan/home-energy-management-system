import { StyleSheet } from 'react-native';
import { SPACING } from '../../../theme/constants';

export const getDeviceAddScreenStyles = (COLORS: any) => StyleSheet.create({
  container: {
    flex: 1,
    backgroundColor: COLORS.dark,
  },
  scrollContainer: {
    flexGrow: 1,
    padding: SPACING.lg,
  },
  mainCard: {
    backgroundColor: COLORS.surface,
    borderRadius: 20,
    padding: SPACING.xl,
    shadowColor: COLORS.black,
    shadowOffset: { width: 0, height: 4 },
    shadowOpacity: 0.1,
    shadowRadius: 12,
    elevation: 8,
  },
  header: {
    flexDirection: 'row' as const,
    alignItems: 'center' as const,
    marginBottom: SPACING.lg,
  },
  headerContent: {
    flex: 1,
    marginLeft: SPACING.md,
  },
  title: {
    fontSize: 24,
    fontWeight: '700' as const,
    color: COLORS.textSoft,
    marginBottom: SPACING.xs,
  },
  subtitle: {
    fontSize: 14,
    color: COLORS.textSoft,
    lineHeight: 20,
  },
  siteInfo: {
    backgroundColor: COLORS.accent + '10',
    borderRadius: 12,
    padding: SPACING.lg,
    marginBottom: SPACING.xl,
    borderWidth: 1,
    borderColor: COLORS.accent + '30',
  },
  siteHeader: {
    flexDirection: 'row' as const,
    alignItems: 'center' as const,
    marginBottom: SPACING.sm,
  },
  siteIcon: {
    width: 32,
    height: 32,
    borderRadius: 16,
    backgroundColor: COLORS.accent + '20',
    justifyContent: 'center' as const,
    alignItems: 'center' as const,
    marginRight: SPACING.sm,
  },
  siteName: {
    fontSize: 16,
    fontWeight: '600' as const,
    color: COLORS.textSoft,
    flex: 1,
  },
  siteAddress: {
    fontSize: 12,
    color: COLORS.textSoft,
    marginBottom: SPACING.xs,
  },
  deviceStatus: {
    backgroundColor: COLORS.warning + '20',
    borderRadius: 12,
    paddingHorizontal: SPACING.sm,
    paddingVertical: SPACING.xs,
    alignSelf: 'flex-start' as const,
  },
  deviceStatusText: {
    fontSize: 11,
    fontWeight: '600' as const,
    color: COLORS.warning,
    textTransform: 'uppercase' as const,
  },
  formWrapper: {
    gap: SPACING.lg,
  },
  buttonContainer: {
    marginTop: SPACING.xl,
    gap: SPACING.md,
  },
});
