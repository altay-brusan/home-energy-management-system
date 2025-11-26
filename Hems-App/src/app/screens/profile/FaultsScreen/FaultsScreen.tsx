import React, { useMemo, useRef, useState, useEffect } from 'react';
import { View, Text, FlatList, Pressable, Animated, RefreshControl } from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../../store/themeStore';
import { getThemeColors } from '../../../../theme/constants';
import { makeFaultsScreenStyles } from './FaultsScreen.styles';
import { ArrowLeft } from 'lucide-react-native';
import Button from '../../../../components/ui/Button';
import { useFaultsStore, Fault } from '../../../../store/faultsStore';
import { useI18n } from '../../../../hooks/useI18n';

type FaultStatus = 'in_progress' | 'processing' | 'completed';

export default function FaultsScreen({ navigation }: any) {
    const insets = useSafeAreaInsets();
    const isDark = useThemeStore(s => s.mode) === 'dark';
    const COLORS = getThemeColors(isDark);
    const styles = useMemo(() => makeFaultsScreenStyles(COLORS, isDark), [isDark]);
    const { t } = useI18n();

    const faults = useFaultsStore(s => s.faults);
    const [refreshing, setRefreshing] = useState(false);
    const [filter, setFilter] = useState<'all' | FaultStatus>('all');

    const opacity = useRef(new Animated.Value(0)).current;
    const translateY = useRef(new Animated.Value(12)).current;

    useEffect(() => {
        Animated.parallel([
            Animated.timing(opacity, { toValue: 1, duration: 220, useNativeDriver: true }),
            Animated.timing(translateY, { toValue: 0, duration: 220, useNativeDriver: true }),
        ]).start();
    }, []);

    const statusColors: Record<FaultStatus, string> = {
        in_progress: '#EB5757',
        processing: '#F2994A',
        completed: '#27AE60',
    };

    const statusLabels: Record<FaultStatus, string> = {
        in_progress: t('troubleshooting.faultsScreen.status.inProgress'),
        processing: t('troubleshooting.faultsScreen.status.processing'),
        completed: t('troubleshooting.faultsScreen.status.completed'),
    };

    const openDetail = (f: Fault) => {
        navigation.navigate('FaultDetail', { item: f });
    };

    const onRefresh = async () => {
        setRefreshing(true);
        setTimeout(() => setRefreshing(false), 1000);
    };

    const mockFaults: Fault[] = [
        {
            id: 'mock-inprogress',
            faultType: 'Refrigerator',
            description: 'Refrigerator is making unusual noise while running.',
            name: 'Demo User 3',
            phone: '555-777-888',
            address: 'Sample Road 789',
            zip: '36000',
            email: 'demo3@example.com',
            status: 'in_progress',
            createdAt: new Date(Date.now() - 1000 * 60 * 30).toISOString(),
        },
        {
            id: 'mock-processing',
            faultType: 'Heating System',
            description: 'Heating system is not reaching desired temperature.',
            name: 'Demo User',
            phone: '555-111-222',
            address: 'Demo Street 123',
            zip: '34000',
            email: 'demo@example.com',
            status: 'processing',
            createdAt: new Date().toISOString(),
        },
        {
            id: 'mock-completed',
            faultType: 'Battery',
            description: 'Battery system is not reaching desired temperature.',
            name: 'Demo User 2',
            phone: '555-333-444',
            address: 'Example Avenue 456',
            zip: '35000',
            email: 'demo2@example.com',
            status: 'completed',
            createdAt: new Date(Date.now() - 1000 * 60 * 60 * 24).toISOString(),
            finishedAt: new Date().toISOString(),
        },
    ];

    const demoFaults = [...faults, ...mockFaults];

    const filteredFaults =
        filter === 'all' ? demoFaults : demoFaults.filter(f => f.status === filter);

    const renderItem = ({ item }: { item: Fault }) => {
        const created = new Date(item.createdAt).toLocaleString();
        const finished = item.finishedAt ? new Date(item.finishedAt).toLocaleString() : null;

        return (
            <Pressable onPress={() => openDetail(item)} style={styles.itemPress}>
                <View style={styles.itemRow}>
                    {/* Status indicator dot - sol tarafta */}
                    <View style={{ marginRight: 12, paddingTop: 2 }}>
                        <View style={{
                            width: 8,
                            height: 8,
                            borderRadius: 4,
                            backgroundColor: statusColors[item.status]
                        }} />
                    </View>

                    <View style={styles.itemContent}>
                        <View style={styles.itemHeader}>
                            <Text style={styles.itemTitle}>{item.faultType}</Text>
                            <View style={[styles.statusBadge, { backgroundColor: statusColors[item.status] }]}>
                                <Text style={styles.statusBadgeText}>{statusLabels[item.status]}</Text>
                            </View>
                        </View>

                        <Text style={styles.itemMsg} numberOfLines={2}>{item.description}</Text>

                        <View style={styles.itemMetaRow}>
                            {item.status === 'completed' && finished ? (
                                <Text style={styles.itemMeta}>{t('troubleshooting.faultsScreen.finishedAt', { date: finished })}</Text>
                            ) : (
                                <Text style={styles.itemMeta}>{t('troubleshooting.faultsScreen.createdAt', { date: created })}</Text>
                            )}
                        </View>
                    </View>
                </View>
            </Pressable>
        );
    };

    const ListHeader = (
        <>
            {/* Header - Notifications benzeri */}
            <View style={styles.headerRow}>
                <View style={{ flexDirection: 'row', alignItems: 'center', gap: 16 }}>
                    <Pressable
                        onPress={() => navigation.goBack()}
                        style={{
                            paddingVertical: 8,
                            paddingRight: 8,
                            marginLeft: -4
                        }}
                    >
                        <ArrowLeft size={26} color={COLORS.text} strokeWidth={2.5} />
                    </Pressable>
                    <Text style={styles.screenTitle}>{t('troubleshooting.faultsScreen.title')}</Text>
                </View>
            </View>

            <View style={styles.warrantyCard}>
                <Text style={styles.warrantyText}>{t('troubleshooting.faultsScreen.warrantyValid', { date: '12/12/2026' })}</Text>
            </View>

            <Button
                title={t('troubleshooting.faultsScreen.createFaultRecord')}
                style={styles.createBtn}
                textStyle={styles.createBtnText}
                onPress={() => navigation.navigate('TroubleShooting')}
            />

            {/* Filter Chips - Notifications benzeri */}
            <View style={styles.filterRow}>
                {['all', 'in_progress', 'processing', 'completed'].map((key) => (
                    <Pressable
                        key={key}
                        onPress={() => setFilter(key as any)}
                        style={[styles.filterChip, filter === key && styles.filterChipActive]}
                    >
                        <Text style={[styles.filterText, filter === key && styles.filterTextActive]}>
                            {key === 'all' ? t('troubleshooting.faultsScreen.all') : statusLabels[key as FaultStatus]}
                        </Text>
                    </Pressable>
                ))}
            </View>
        </>
    );

    return (
        <Animated.View
            style={[
                styles.screen,
                { paddingTop: insets.top + 8, opacity, transform: [{ translateY }] },
            ]}
        >
            <FlatList<Fault>
                data={filteredFaults}
                keyExtractor={(f) => f.id}
                renderItem={renderItem}
                contentContainerStyle={[styles.pageScroll, { paddingBottom: insets.bottom + 40 }]}
                ItemSeparatorComponent={() => <View style={styles.separator} />}
                showsVerticalScrollIndicator={false}
                ListHeaderComponent={ListHeader}
                refreshControl={
                    <RefreshControl
                        refreshing={refreshing}
                        onRefresh={onRefresh}
                        colors={[COLORS.accent]}
                        tintColor={COLORS.accent}
                    />
                }
            />
        </Animated.View>
    );
}
