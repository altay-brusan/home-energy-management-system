import React, { useState, useRef, useImperativeHandle, forwardRef } from 'react';
import { Pressable, Text, View } from 'react-native';
import { Swipeable } from 'react-native-gesture-handler';
import FontAwesome from '@react-native-vector-icons/fontawesome';
import type { makeTrashScreenStyles } from '../TrashScreen.styles';
import { TrashNotification } from '../../../../hooks/useNotifications';
import StatusPill from './StatusPill';
import { useI18n } from '../../../../hooks/useI18n';

type Styles = ReturnType<typeof makeTrashScreenStyles>;

type Props = {
  n: TrashNotification;
  styles: Styles;
  color: string;
  onRestore: (id: string) => void;
  onDelete: (id: string) => void;
  onOpen: (n: TrashNotification) => void;
  onSwipeableOpen?: (id: string, closePrevious: () => void) => void;
};

export type TrashItemRef = {
  close: () => void;
};

export const TrashItem = forwardRef<TrashItemRef, Props>(({ n, styles, color, onRestore, onDelete, onOpen, onSwipeableOpen }, ref) => {
  const swipeableRef = useRef<Swipeable>(null);
  const { t } = useI18n();

  useImperativeHandle(ref, () => ({
    close: () => {
      swipeableRef.current?.close();
    },
  }));

  const handleSwipeableOpen = () => {
    if (onSwipeableOpen) {
      onSwipeableOpen(n.id, () => {
        swipeableRef.current?.close();
      });
    }
  };

  const handleSwipeableClose = () => {
    // Swipe kapandığında işlem yok
  };

  const handleRestore = () => {
    onRestore(n.id);
    swipeableRef.current?.close();
  };

  const handleDelete = () => {
    onDelete(n.id);
    swipeableRef.current?.close();
  };

  return (
    <Swipeable
      ref={swipeableRef}
      renderLeftActions={() => (
        <Pressable onPress={handleRestore} style={styles.swipeLeft}>
          <View style={styles.swipeButtonContent}>
            <FontAwesome name="undo" size={18} color="#fff" style={styles.swipeIcon} />
            <Text style={styles.swipeLeftText}>{t('trash.restore')}</Text>
          </View>
        </Pressable>
      )}
      renderRightActions={() => (
        <Pressable onPress={handleDelete} style={styles.swipeRight}>
          <View style={styles.swipeButtonContent}>
            <FontAwesome name="trash" size={18} color="#fff" style={styles.swipeIcon} />
            <Text style={styles.swipeRightText}>{t('common.delete')}</Text>
          </View>
        </Pressable>
      )}
        onSwipeableWillClose={handleSwipeableClose}
        onSwipeableWillOpen={handleSwipeableOpen}
        overshootLeft={false}
        overshootRight={false}
        friction={2}
      >
        <Pressable onPress={() => onOpen(n)} style={styles.itemPress}>
          <View style={[styles.itemRow, { borderLeftColor: color }]}>
          <View style={styles.itemContent}>
            <View style={styles.itemHeader}>
              <Text 
                style={styles.itemTitle}
                numberOfLines={2}
                ellipsizeMode="tail"
              >
                {n.title}
              </Text>
              <StatusPill styles={styles} />
            </View>
            <Text style={styles.itemMsg} numberOfLines={2}>{n.message}</Text>
            <View style={styles.itemMetaRow}>
              <View style={styles.metaLeft}>
                {n.severity === 'critical' && <Text style={styles.criticalText}>{t('common.critical')}</Text>}
              </View>
              <Text style={styles.itemMeta}>{t('trash.deletedAt', { date: n.deletedAt })}</Text>
            </View>
          </View>
        </View>
      </Pressable>
    </Swipeable>
  );
});

TrashItem.displayName = 'TrashItem';

export default TrashItem;
