import { StyleSheet } from 'react-native';
import { SPACING, TYPOGRAPHY } from '../../../theme/constants';

export function getDeviceListStyles (COLORS: Record<string, string>) {
  return StyleSheet.create({
    screen: { flex: 1, backgroundColor: COLORS.dark },
    container: { paddingHorizontal: SPACING.md, paddingTop: SPACING.md, paddingBottom: SPACING.lg },
    title: { color: COLORS.textSoft, fontSize: TYPOGRAPHY.xl, fontWeight: '800', marginBottom: SPACING.sm },
    grid: { gap: SPACING.sm },
    card: {
      width: '100%',
      backgroundColor: COLORS.surface,
      borderRadius: 14,
      borderWidth: 1,
      borderColor: COLORS.border,
      overflow: 'hidden',
      flexDirection: 'row',
      alignItems: 'center',
      padding: SPACING.sm,
    },
    thumbWrap: {
      width: 80,
      height: 80,
      borderRadius: 12,
      overflow: 'hidden',
      backgroundColor: COLORS.bgShadeDark,
      borderWidth: 1,
      borderColor: COLORS.border,
      alignItems: 'center',
      justifyContent: 'center',
    },
    image: { width: '90%', height: '90%' },
    body: { flex: 1, paddingLeft: SPACING.md },
    name: { color: COLORS.textSoft, fontWeight: '800', marginBottom: 2 },
    serial: { color: COLORS.slate400, fontSize: TYPOGRAPHY.xs },
    row: { flexDirection: 'row', alignItems: 'center', justifyContent: 'space-between', marginTop: 6 },
    dot: { width: 8, height: 8, borderRadius: 999, marginRight: 6 },
    chip: { paddingVertical: 6, paddingHorizontal: 8, borderRadius: 999, backgroundColor: COLORS.bgShadeLight, borderWidth: 1, borderColor: COLORS.border },
    chipText: { color: COLORS.textSoft, fontSize: TYPOGRAPHY.xs, fontWeight: '700' },
    rightIcon: { marginLeft: SPACING.sm },
  });
}
