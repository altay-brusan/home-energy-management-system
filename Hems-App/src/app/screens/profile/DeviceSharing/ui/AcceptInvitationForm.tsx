import React, { useState, useCallback } from 'react';
import { View, Text, ActivityIndicator } from 'react-native';
import Input from '../../../../../components/forms/Input';
import Button from '../../../../../components/ui/Button/Button';
import { useI18n } from '../../../../../hooks/useI18n';
import { invitationsService } from '../../../../../api/services/devicesharingService';
import { useSitesStore } from '../../../../../store/sitesStore';
import { navigationRef } from '../../../../routes';

type Props = {
  COLORS: any;
  styles: any;
};

export function AcceptInvitationForm({ COLORS, styles }: Props) {
  const { t } = useI18n();
  const { getUserSites, setSelectedSite, sites: currentSites } = useSitesStore();

  const [code, setCode] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');
  const [success, setSuccess] = useState('');

  const handleAcceptInvitation = useCallback(async () => {
    if (!code.trim()) {
      setError(t('deviceSharing.acceptInvitation.enterCode'));
      return;
    }

    // Validate 6-digit code
    const cleanCode = code.trim();
    if (!/^\d{6}$/.test(cleanCode)) {
      setError(t('deviceSharing.acceptInvitation.invalidCodeFormat'));
      return;
    }

    setLoading(true);
    setError('');
    setSuccess('');

    try {
      // Store current site IDs to find the new one after refresh
      const previousSiteIds = new Set(currentSites.map(s => s.id));

      await invitationsService.acceptInvitation({ token: cleanCode });

      // Refresh sites to get the newly shared site
      const updatedDashboard = await getUserSites();
      const updatedSites = updatedDashboard?.siteList || [];

      // Find the newly added site
      const newSite = updatedSites.find(s => !previousSiteIds.has(s.id));

      if (newSite) {
        setSelectedSite(newSite);
      }

      setSuccess(t('invitation.accepted'));
      setCode('');

      // Navigate to MainTabs to show the new site
      setTimeout(() => {
        navigationRef.current?.reset({
          index: 0,
          routes: [{ name: 'MainTabs' }],
        });
      }, 1500);
    } catch (err: any) {
      const errorMessage = err?.response?.data?.message || err?.message || t('invitation.acceptFailed');
      setError(errorMessage);
    } finally {
      setLoading(false);
    }
  }, [code, currentSites, getUserSites, setSelectedSite, t]);

  return (
    <View style={[styles.inputGroup, { marginTop: 24 }]}>
      <View style={{
        backgroundColor: COLORS.surface || COLORS.gray100,
        borderRadius: 12,
        padding: 16,
        borderWidth: 1,
        borderColor: COLORS.border || COLORS.gray200,
      }}>
        <Text style={{
          fontSize: 14,
          fontWeight: '600',
          color: COLORS.text,
          marginBottom: 12,
        }}>
          {t('deviceSharing.acceptInvitation.title')}
        </Text>

        <Input
          placeholder={t('deviceSharing.acceptInvitation.placeholder')}
          placeholderTextColor={COLORS.slate400}
          keyboardType="number-pad"
          maxLength={6}
          value={code}
          onChangeText={(text) => {
            setCode(text.replace(/[^0-9]/g, ''));
            setError('');
            setSuccess('');
          }}
        />

        <Button
          title={loading ? t('common.loading') : t('deviceSharing.acceptInvitation.button')}
          style={[styles.saveBtn, { marginTop: 12 }]}
          textStyle={styles.saveBtnText}
          disabled={loading || !code.trim()}
          onPress={handleAcceptInvitation}
        />

        {(!!error || !!success) && (
          <View style={[styles.messageBox, { marginTop: 12, marginBottom: 0 }]}>
            <Text
              style={[
                styles.messageText,
                error ? styles.errorText : styles.successText,
              ]}
            >
              {error || success}
            </Text>
          </View>
        )}

        {loading && (
          <View style={{ marginTop: 10 }}>
            <ActivityIndicator color={COLORS.accent} />
          </View>
        )}
      </View>
    </View>
  );
}

export default AcceptInvitationForm;
