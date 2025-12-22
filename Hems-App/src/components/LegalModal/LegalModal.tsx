import React from 'react';
import {
  Modal,
  View,
  Text,
  ScrollView,
  TouchableOpacity,
  Pressable,
} from 'react-native';
import { useTranslation } from 'react-i18next';
import { useThemeStore } from '../../store/themeStore';
import { getThemeColors } from '../../theme/constants';
import { makeLegalModalStyles } from './LegalModal.styles';

export interface LegalModalProps {
  visible: boolean;
  type: 'privacy' | 'terms';
  onClose: () => void;
}

export function LegalModal({ visible, type, onClose }: LegalModalProps) {
  const { t } = useTranslation();
  const mode = useThemeStore(s => s.mode);
  const isDark = mode === 'dark';
  const COLORS = getThemeColors(isDark);
  const styles = React.useMemo(
    () => makeLegalModalStyles(COLORS, isDark),
    [COLORS, isDark]
  );

  const title = type === 'privacy'
    ? t('legal.privacyPolicyTitle')
    : t('legal.termsOfServiceTitle');

  const renderPrivacyPolicy = () => (
    <>
      <Text style={styles.lastUpdated}>{t('legal.lastUpdated')}</Text>
      <Text style={styles.intro}>{t('legal.privacyPolicy.intro')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.privacyPolicy.dataCollectedTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.privacyPolicy.dataCollectedContent')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.privacyPolicy.purposeTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.privacyPolicy.purposeContent')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.privacyPolicy.legalBasisTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.privacyPolicy.legalBasisContent')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.privacyPolicy.thirdPartyTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.privacyPolicy.thirdPartyContent')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.privacyPolicy.dataRetentionTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.privacyPolicy.dataRetentionContent')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.privacyPolicy.userRightsTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.privacyPolicy.userRightsContent')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.privacyPolicy.securityTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.privacyPolicy.securityContent')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.privacyPolicy.contactTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.privacyPolicy.contactContent')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.privacyPolicy.changesTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.privacyPolicy.changesContent')}</Text>
    </>
  );

  const renderTermsOfService = () => (
    <>
      <Text style={styles.lastUpdated}>{t('legal.lastUpdated')}</Text>
      <Text style={styles.intro}>{t('legal.termsOfService.intro')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.termsOfService.serviceDescriptionTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.termsOfService.serviceDescriptionContent')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.termsOfService.userAccountTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.termsOfService.userAccountContent')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.termsOfService.userResponsibilitiesTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.termsOfService.userResponsibilitiesContent')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.termsOfService.intellectualPropertyTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.termsOfService.intellectualPropertyContent')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.termsOfService.serviceAvailabilityTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.termsOfService.serviceAvailabilityContent')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.termsOfService.liabilityTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.termsOfService.liabilityContent')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.termsOfService.terminationTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.termsOfService.terminationContent')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.termsOfService.governingLawTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.termsOfService.governingLawContent')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.termsOfService.changesTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.termsOfService.changesContent')}</Text>

      <Text style={styles.sectionTitle}>{t('legal.termsOfService.contactTitle')}</Text>
      <Text style={styles.sectionContent}>{t('legal.termsOfService.contactContent')}</Text>
    </>
  );

  return (
    <Modal
      visible={visible}
      transparent
      animationType="fade"
      onRequestClose={onClose}
    >
      <Pressable style={styles.overlay} onPress={onClose}>
        <View style={styles.container}>
          <View style={styles.header}>
            <Text style={styles.title}>{title}</Text>
            <TouchableOpacity style={styles.closeButton} onPress={onClose}>
              <Text style={styles.closeButtonText}>✕</Text>
            </TouchableOpacity>
          </View>

          <ScrollView
            style={styles.scrollView}
            contentContainerStyle={styles.scrollContent}
            showsVerticalScrollIndicator={true}
            nestedScrollEnabled={true}
            bounces={true}
          >
            {type === 'privacy' ? renderPrivacyPolicy() : renderTermsOfService()}
          </ScrollView>

          <View style={styles.footer}>
            <TouchableOpacity style={styles.footerButton} onPress={onClose}>
              <Text style={styles.footerButtonText}>{t('common.close')}</Text>
            </TouchableOpacity>
          </View>
        </View>
      </Pressable>
    </Modal>
  );
}
