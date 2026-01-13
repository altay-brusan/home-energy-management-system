import { StyleSheet, Dimensions } from 'react-native';
import { SPACING } from '../../../theme/constants';

export const makeProfileScreenStyles = (COLORS: Record<string, string>, isDark: boolean) => {
  const width = Dimensions.get('window').width;
  const isSmall = width <= 380;
  const isVerySmall = width <= 340;

  return StyleSheet.create({
  container: {
    flexGrow: 1,
    backgroundColor: COLORS.dark,
    paddingHorizontal: 0,
    paddingTop: SPACING.xl,
    paddingBottom: SPACING.xl,
    justifyContent: 'flex-start',
    minHeight: '100%',
  },
  avatarContainer: {
    alignItems: 'center',
    marginBottom: SPACING.xl,
    width: '100%',
    paddingHorizontal: SPACING.lg,
  },
  avatar: {
    width: 100,
    height: 100,
    borderRadius: 50,
    marginBottom: SPACING.sm,
    borderWidth: 3,
    borderColor: COLORS.accent,
    backgroundColor: COLORS.bgShadeLight,
    shadowColor: COLORS.accent,
    shadowOpacity: 0.1,
    shadowRadius: 10,
    shadowOffset: { width: 0, height: 2 },
    elevation: 3,
  },
  email: {
    fontSize: 17,
    fontWeight: '600',
    color: COLORS.textSoft,
    letterSpacing: 0.1,
    marginTop: 2,
  },
  cardList: {
    width: '100%',
    marginBottom: SPACING.xl,
  },
  card: {
    width: '100%',
    backgroundColor: isDark ? 'rgba(32, 40, 53, 1)' : '#FFFFFF',
    borderBottomWidth: 1,
    borderBottomColor: isDark ? 'rgba(255,255,255,0.08)' : 'rgba(0,0,0,0.08)',
    paddingVertical: 16,
    paddingHorizontal: SPACING.lg,
    marginBottom: 0,
    alignItems: 'center',
    flexDirection: 'row',
    minHeight: 56,
  },
  sectionTitle: {
    fontSize: 15,
    fontWeight: '400',
    color: COLORS.textSoft,
    flex: 1,
    flexShrink: 1,
  },
  tabButton: {
    minWidth: 80,
    paddingHorizontal: SPACING.md,
    paddingVertical: SPACING.sm,
    borderRadius: 8,
    borderWidth: 1,
    borderColor: COLORS.border,
  },
  tabButtonText: {
    color: COLORS.textSoft,
    fontWeight: '500',
    fontSize: 14,
  },
  topBar: {
    flexDirection: 'row',
    alignItems: 'center',
    justifyContent: 'space-between',
    width: '100%',
    paddingHorizontal: 12,
    marginBottom: 8,
  },
  regionWrap: {
    marginLeft: 8,
    alignItems: 'center',
    justifyContent: 'center',
  },
  versionText: {
    color: COLORS.slate400,
    marginTop: SPACING.lg,
    textAlign: 'center',
    paddingHorizontal: SPACING.lg,
  },
  });
};