import React, { useState } from 'react';
import {
    ScrollView,
    Text,
    View,
    Pressable,
    TextInput,
    LayoutAnimation,
    Platform,
    UIManager,
} from 'react-native';
import { useSafeAreaInsets } from 'react-native-safe-area-context';
import { useThemeStore } from '../../../../store/themeStore';
import { getThemeColors } from '../../../../theme/constants';
import { makeFaultDetailStyles } from './FaultDetailScreen.styles';
import MaterialIcons from '@react-native-vector-icons/material-icons';
import Button from '../../../../components/ui/Button';
import { useI18n } from '../../../../hooks/useI18n';

if (Platform.OS === 'android' && UIManager.setLayoutAnimationEnabledExperimental) {
    UIManager.setLayoutAnimationEnabledExperimental(true);
}

type Fault = {
    id: string;
    faultType: string;
    description: string;
    name: string;
    phone: string;
    address: string;
    zip: string;
    email: string;
    status: 'in_progress' | 'processing' | 'completed';
    createdAt: string;
    finishedAt?: string;
    technicianNote?: string;
};

type InfoRowProps = {
    icon: string;
    label: string;
    value: string;
    COLORS: Record<string, string>;
    isLast?: boolean;
    styles: any;
};

type TimelineItemProps = {
    icon: string;
    label: string;
    value: string;
    COLORS: Record<string, string>;
    isLast?: boolean;
    styles: any;
};

export default function FaultDetailScreen({ navigation, route }: any) {
    const { item }: { item: Fault } = route.params;
 const { t } = useI18n();
    const mockItem: Fault = {
        ...item,
      technicianNote:
  item.technicianNote ||
  `${t('troubleshooting.faultDetail.mockNotes.line1')}\n${t('troubleshooting.faultDetail.mockNotes.line2')}\n${t('troubleshooting.faultDetail.mockNotes.line3')}`,

    };

    const insets = useSafeAreaInsets();
    const mode = useThemeStore((s) => s.mode);
    const isDark = mode === 'dark';
    const COLORS = getThemeColors(isDark);
    const styles = makeFaultDetailStyles(COLORS, isDark);
  

    const [expanded, setExpanded] = useState(false);
    const [notes, setNotes] = useState<{ text: string; requests: string[] }[]>(
        (mockItem.technicianNote ?? '')
            .split('\n')
            .map((n) => ({ text: n, requests: [] }))
    );
    const [activeNote, setActiveNote] = useState<number | null>(null);
    const [newRequest, setNewRequest] = useState('');

    const toggleExpand = () => {
        LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
        setExpanded(!expanded);
    };

    const toggleRequestInput = (idx: number) => {
        LayoutAnimation.configureNext(LayoutAnimation.Presets.easeInEaseOut);
        setActiveNote(activeNote === idx ? null : idx);
        setNewRequest('');
    };

    const addRequest = (idx: number) => {
        if (newRequest.trim()) {
            const updatedNotes = [...notes];
            updatedNotes[idx].requests.push(newRequest.trim());
            setNotes(updatedNotes);
            setNewRequest('');
        }
    };

    const getStatusConfig = (status: Fault['status']) => {
        switch (status) {
            case 'completed':
                return { color: '#10B981', icon: 'check-circle', label: t('troubleshooting.faultsScreen.status.completed') };
            case 'processing':
                return { color: '#F59E0B', icon: 'schedule', label: t('troubleshooting.faultsScreen.status.processing') };
            case 'in_progress':
                return { color: '#EB5757', icon: 'build-circle', label: t('troubleshooting.faultsScreen.status.inProgress') };
            default:
                return { color: '#6B7280', icon: 'help-outline', label: status };
        }
    };

    const statusConfig = getStatusConfig(mockItem.status);
    const textColor = isDark ? COLORS.white : COLORS.black;

    return (
        <View style={[styles.screen, { paddingTop: insets.top }]}>
            <View style={styles.headerRow}>
                <Pressable style={styles.backButton} onPress={() => navigation.goBack()}>
                    <MaterialIcons name="arrow-back" size={24} color={textColor} />
                </Pressable>
                <Text style={[styles.screenTitle, { color: textColor }]}>
                    {t('troubleshooting.faultDetail.title')}
                </Text>
                <Pressable style={styles.moreButton}>
                    <MaterialIcons name="more-vert" size={24} color={textColor} />
                </Pressable>
            </View>

            <ScrollView
                style={styles.container}
                contentContainerStyle={{ paddingBottom: insets.bottom + 24 }}
                showsVerticalScrollIndicator={false}
            >
                <View style={styles.heroCard}>
                    <View
                        style={[
                            styles.statusBadge,
                            { backgroundColor: statusConfig.color + '20' },
                        ]}
                    >
                        <View
                            style={[
                                styles.iconCircleSm,
                                { backgroundColor: statusConfig.color + '20' },
                            ]}
                        >
                            <MaterialIcons
                                name={statusConfig.icon as any}
                                size={20}
                                color={statusConfig.color}
                            />
                        </View>
                        <Text
                            style={[styles.statusText, { color: statusConfig.color }]}
                        >
                            {statusConfig.label}
                        </Text>
                    </View>

                    <View style={styles.iconCircle}>
                        <MaterialIcons
                            name="error-outline"
                            size={34}
                            color={COLORS.accent}
                        />
                    </View>
                    <Text style={styles.faultType}>{mockItem.faultType}</Text>
                    <Text style={styles.description}>{mockItem.description}</Text>
                </View>

                <View style={styles.section}>
                    <Text style={styles.sectionTitle}>{t('troubleshooting.faultDetail.contactInformation')}</Text>
                    <View style={styles.sectionCard}>
                        <InfoRow
                            icon="person"
                            label={t('troubleshooting.faultDetail.name')}
                            value={mockItem.name}
                            COLORS={COLORS}
                            styles={styles}
                        />
                        <InfoRow
                            icon="email"
                            label={t('troubleshooting.faultDetail.email')}
                            value={mockItem.email}
                            COLORS={COLORS}
                            styles={styles}
                        />
                        <InfoRow
                            icon="phone"
                            label={t('troubleshooting.faultDetail.phone')}
                            value={mockItem.phone}
                            COLORS={COLORS}
                            isLast
                            styles={styles}
                        />
                    </View>
                </View>

                <View style={styles.section}>
                    <Text style={styles.sectionTitle}>{t('troubleshooting.faultDetail.location')}</Text>
                    <View style={styles.sectionCard}>
                        <InfoRow
                            icon="location-on"
                            label={t('troubleshooting.faultDetail.address')}
                            value={mockItem.address}
                            COLORS={COLORS}
                            styles={styles}
                        />
                        <InfoRow
                            icon="local-post-office"
                            label={t('troubleshooting.faultDetail.zipCode')}
                            value={mockItem.zip}
                            COLORS={COLORS}
                            isLast
                            styles={styles}
                        />
                    </View>
                </View>

                <View style={styles.section}>
                    <Text style={styles.sectionTitle}>{t('troubleshooting.faultDetail.timeline')}</Text>
                    <View style={styles.sectionCard}>
                        <TimelineItem
                            icon="event"
                            label={t('troubleshooting.faultDetail.created')}
                            value={new Date(mockItem.createdAt).toLocaleString()}
                            COLORS={COLORS}
                            styles={styles}
                        />
                        {mockItem.status === 'completed' && mockItem.finishedAt && (
                            <TimelineItem
                                icon="event-available"
                                label={t('troubleshooting.faultDetail.completed')}
                                value={new Date(mockItem.finishedAt).toLocaleString()}
                                COLORS={COLORS}
                                isLast
                                styles={styles}
                            />
                        )}
                    </View>
                </View>

                <View style={styles.section}>
                    <Pressable style={styles.sectionCard} onPress={toggleExpand}>
                        <View style={styles.noteHeader}>
                            <MaterialIcons
                                name="sticky-note-2"
                                size={22}
                                color={COLORS.accent}
                            />
                            <Text
                                style={[styles.sectionTitle, { marginLeft: 8 }]}
                            >
                                {t('troubleshooting.faultDetail.technicianNotes')}
                            </Text>
                            <MaterialIcons
                                name={expanded ? 'expand-less' : 'expand-more'}
                                size={24}
                                color={COLORS.textSoft}
                                style={{ marginLeft: 'auto' }}
                            />
                        </View>
                        {expanded && (
                            <View style={{ marginTop: 12 }}>
                                {notes.map((note, idx) => (
                                    <View
                                        key={idx}
                                        style={[
                                            styles.noteItem,
                                            expanded && styles.noteItemExpanded,
                                        ]}
                                    >
                                        <Pressable
                                            style={styles.notePressable}
                                            onPress={() => toggleRequestInput(idx)}
                                        >
                                            <MaterialIcons
                                                name="chevron-right"
                                                size={20}
                                                color={COLORS.accent}
                                            />
                                            <Text style={styles.noteTextClickable}>
                                                {note.text}
                                            </Text>
                                        </Pressable>

                                        {activeNote === idx && (
                                            <View style={styles.requestContainer}>
                                                {note.requests.map((req, rIdx) => (
                                                    <View key={rIdx} style={styles.requestCard}>
                                                        <Text style={styles.requestText}>{req}</Text>
                                                    </View>
                                                ))}
                                                <View style={styles.newNoteRow}>
                                                    <TextInput
                                                        style={styles.noteInputBig}
                                                        placeholder={t('troubleshooting.faultDetail.createRequest')}
                                                        placeholderTextColor={COLORS.textSoft}
                                                        value={newRequest}
                                                        onChangeText={setNewRequest}
                                                    />
                                                    <Pressable
                                                        style={styles.addBtnBig}
                                                        onPress={() => addRequest(idx)}
                                                    >
                                                        <MaterialIcons
                                                            name="send"
                                                            size={22}
                                                            color={COLORS.white}
                                                        />
                                                    </Pressable>
                                                </View>
                                            </View>
                                        )}
                                    </View>
                                ))}
                            </View>
                        )}
                    </Pressable>
                </View>
            </ScrollView>
        </View>
    );
}

const InfoRow: React.FC<InfoRowProps> = ({
    icon,
    label,
    value,
    COLORS,
    isLast = false,
    styles,
}) => (
    <View style={[styles.infoRow, isLast && styles.infoRowLast]}>
        <View style={styles.infoLabelContainer}>
            <View
                style={[
                    styles.iconCircleSm,
                    { backgroundColor: COLORS.accent + '20' },
                ]}
            >
                <MaterialIcons name={icon as any} size={20} color={COLORS.accent} />
            </View>
            <Text style={[styles.infoLabel, { fontSize: 14 }]}>{label}</Text>
        </View>
        <Text style={[styles.infoValue, { fontSize: 16 }]}>{value}</Text>
    </View>
);

const TimelineItem: React.FC<TimelineItemProps> = ({
    icon,
    label,
    value,
    COLORS,
    isLast = false,
    styles,
}) => (
    <View style={[styles.timelineItem, isLast && styles.timelineItemLast]}>
        <View style={styles.timelineIconContainer}>
            <View
                style={[
                    styles.timelineCircle,
                    { backgroundColor: COLORS.accent + '20' },
                ]}
            >
                <MaterialIcons name={icon as any} size={20} color={COLORS.accent} />
            </View>
            {!isLast && (
                <View
                    style={[styles.timelineLine, { backgroundColor: COLORS.border }]}
                />
            )}
        </View>
        <View style={styles.timelineContent}>
            <Text style={[styles.timelineLabel, { fontSize: 14 }]}>{label}</Text>
            <Text style={[styles.timelineValue, { fontSize: 16 }]}>{value}</Text>
        </View>
    </View>
);
