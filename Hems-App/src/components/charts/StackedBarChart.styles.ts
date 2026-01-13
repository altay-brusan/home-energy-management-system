/**
 * Stacked Bar Chart Styles
 */

import { StyleSheet } from 'react-native';

export const styles = StyleSheet.create({
  container: {
    width: '100%',
    alignItems: 'center',
    paddingVertical: 10
  },

  legend: {
    width: '100%',
    paddingHorizontal: 20,
    paddingVertical: 10,
    marginTop: 10
  },

  legendRow: {
    flexDirection: 'row',
    flexWrap: 'wrap',
    justifyContent: 'center',
    marginBottom: 8
  },

  legendItem: {
    flexDirection: 'row',
    alignItems: 'center',
    marginRight: 12,
    marginBottom: 4
  },

  legendColor: {
    width: 16,
    height: 16,
    borderRadius: 8,
    marginRight: 6
  },

  legendText: {
    fontSize: 11,
    color: '#888'
  }
});
