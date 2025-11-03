import { StyleSheet } from 'react-native';

export function getDashboardHeroImageStyles (COLORS: Record<string, string>) {
  return StyleSheet.create({
    root: {
      width: '100%',
      backgroundColor: COLORS.surface,
      borderRadius: 15,
      borderWidth: 1,
      borderColor: COLORS.border,
      marginBottom: 20,
      marginTop: 15,
    },
    image: {
      width: '90%',
      maxWidth: '100%',
      justifyContent: 'center',
      alignSelf: 'center',
    },
    content: {
      backgroundColor: COLORS.surface,
      borderTopWidth: 1,
      borderTopColor: COLORS.border,
      paddingHorizontal: 10,
      paddingVertical: 10,
      borderRadius: 15,
      alignItems: 'center',
justifyContent: 'center',
    },
    title: {
      color: COLORS.textSoft,
      fontSize: 16,
      fontWeight: '700',
      marginBottom: 2,
      textAlign: 'center',
    },
    subtitle: {
      color: COLORS.slate400,
      fontSize: 12,
      marginBottom: 6,
      textAlign: 'center',
    },
    metaRow: {
      flexDirection: 'row',
      flexWrap: 'wrap',
      gap: 6,
      alignItems: 'center',
      justifyContent: 'center',
    },
    metaItem: {
      color: COLORS.black,
      fontSize: 12,
      backgroundColor: COLORS.accentGlow,
      borderRadius: 12,
      paddingHorizontal: 8,
      paddingVertical: 3,
      marginRight: 4,
      marginBottom: 3,
    },
  });
}
