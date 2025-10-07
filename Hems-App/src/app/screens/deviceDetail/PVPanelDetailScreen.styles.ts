import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../theme/constants';

export function getPVStyles(COLORS: Record<string, string>) {
  return StyleSheet.create({
    screen: { 
      flex: 1, 
      backgroundColor: COLORS.dark 
    },
    header: {
      position: 'absolute',
      top: 0,
      left: 0,
      right: 0,
      zIndex: 10,
      backgroundColor: COLORS.surface,
    },
    headerContent: {
      flexDirection: 'row',
      alignItems: 'center',
      justifyContent: 'space-between',
      paddingHorizontal: SPACING.md,
      paddingVertical: SPACING.sm,
    },
    headerTitle: {
      fontSize: TYPOGRAPHY.md,
      fontWeight: '700',
      color: COLORS.textSoft,
    },
    backButton: {
      width: 36,
      height: 36,
      borderRadius: 18,
      backgroundColor: COLORS.surface,
      alignItems: 'center',
      justifyContent: 'center',
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    container: { 
      padding: SPACING.md,
      paddingTop: SPACING.lg,
      paddingBottom: SPACING.xl 
    },
    card: { 
      backgroundColor: COLORS.dark, 
      borderRadius: 16, 
      padding: SPACING.md, 
      borderWidth: 1, 
      borderColor: COLORS.border, 
      marginBottom: SPACING.md,
      shadowColor: COLORS.black,
      shadowOffset: { width: 0, height: 2 },
      shadowOpacity: 0.08,
      shadowRadius: 6,
      elevation: 3,
    },
    title: { 
      fontSize: TYPOGRAPHY.lg, 
      fontWeight: '800', 
      color: COLORS.textSoft, 
      marginBottom: 10,
      letterSpacing: 0.3,
    },
    sub: { 
      color: COLORS.slate400, 
      marginBottom: 10,
      fontSize: TYPOGRAPHY.sm,
    },
    row: { 
      flexDirection: 'row', 
      gap: SPACING.sm, 
      justifyContent: 'center', 
      marginTop: SPACING.sm,
      marginBottom: SPACING.xs,
    },
    chart: { 
      width: '100%', 
      height: 240, 
      borderRadius: 14, 
      overflow: 'hidden', 
      backgroundColor: COLORS.bgShadeLight, 
      borderWidth: 1, 
      borderColor: COLORS.border,
      shadowColor: COLORS.black,
      shadowOffset: { width: 0, height: 1 },
      shadowOpacity: 0.08,
      shadowRadius: 4,
      elevation: 1,
    },
    tabBtn: { 
      minWidth: 68, 
      paddingVertical: 10, 
      paddingHorizontal: 10,
      borderRadius: 8,
      borderWidth: 1,
      borderColor: COLORS.border,
    },
    statRow: { 
      flexDirection: 'row', 
      justifyContent: 'space-between',
      paddingVertical: SPACING.xs,
      borderBottomWidth: 1,
      borderBottomColor: COLORS.border,
    },
    statText: { 
      color: COLORS.textSoft, 
      fontWeight: '600',
      fontSize: TYPOGRAPHY.sm,
    },
  });
}
