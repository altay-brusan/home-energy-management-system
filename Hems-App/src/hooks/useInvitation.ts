import { useState, useEffect, useCallback, useRef } from 'react';
import { invitationsService } from '../api/services/devicesharingService';
import { useSelectedSite } from './useSelectedSite';
import { InvitationListDto } from '../api/types/devicesharingService';
import { useI18n } from './useI18n';

export function useInvitation() {
  const [email, setEmail] = useState('');
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [loading, setLoading] = useState(false);
  const [invitations, setInvitations] = useState<InvitationListDto | null>(null);
  const selectedSite = useSelectedSite();
  const selectedSiteId = selectedSite?.id;
  const { t } = useI18n();

  const loadInvitationsRef = useRef(false);
  const loadInvitationsRefId = useRef<string | undefined>(undefined);

  const loadInvitations = useCallback(async (force = false) => {
    if (!selectedSiteId) return;
    
    if (!force && loadInvitationsRef.current && loadInvitationsRefId.current === selectedSiteId) {
      return;
    }
    
    if (force) {
      loadInvitationsRef.current = false;
    }
    
    loadInvitationsRef.current = true;
    loadInvitationsRefId.current = selectedSiteId;
    
    try {
      const data = await invitationsService.getInvitationsBySite(selectedSiteId);
      if (data) {
        const newInvitations: InvitationListDto = {
          withUser: Array.isArray(data.withUser) ? [...data.withUser] : [],
          withoutUser: Array.isArray(data.withoutUser) ? [...data.withoutUser] : [],
        };
        if (loadInvitationsRefId.current === selectedSiteId) {
          setInvitations(newInvitations);
          setError('');
        }
      }
    } catch (err) {
      if (loadInvitationsRefId.current === selectedSiteId) {
        setError(t('deviceSharing.invitationListError'));
      }
    } finally {
      if (loadInvitationsRefId.current === selectedSiteId) {
        loadInvitationsRef.current = false;
      }
    }
  }, [selectedSiteId, t]);

  const emailRef = useRef(email);
  useEffect(() => {
    emailRef.current = email;
  }, [email]);

  const sendInvitation = useCallback(async () => {
    if (!selectedSiteId) return setError(t('deviceSharing.noSiteSelected'));
    try {
      setError('');
      setMessage('');
      setLoading(true);
      
      const currentEmail = emailRef.current;
      if (!currentEmail.trim()) {
        setLoading(false);
        return setError(t('deviceSharing.noEmailEntered'));
      }

      const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
      if (!emailRegex.test(currentEmail.trim())) {
        setLoading(false);
        return setError(t('deviceSharing.invalidEmailFormat'));
      }

      await invitationsService.createInvitation({
        email: currentEmail.trim(),
        siteId: selectedSiteId,
      });

      setMessage(t('deviceSharing.invitationSent'));
      setEmail('');
      
      await loadInvitations(true);
    } catch {
      setError(t('deviceSharing.invitationError'));
    } finally {
      setLoading(false);
    }
  }, [selectedSiteId, loadInvitations, t]);

  const removeInvitation = useCallback(async (invitationId: string) => {
    try {
      setError('');
      setMessage('');
      await invitationsService.deleteInvitation(invitationId);
      setMessage(t('deviceSharing.invitationRemoved'));
      
      await loadInvitations(true);
    } catch {
      setError(t('deviceSharing.removeInvitationError'));
    }
  }, [loadInvitations, t]);

  const removeUser = useCallback(async (id: string): Promise<void> => {
    if (!id) {
      setError(t('deviceSharing.removeInvitationError'));
      return;
    }
    try {
      setError('');
      setMessage('');
      await invitationsService.removeUser(id);
      setMessage(t('deviceSharing.invitationRemoved'));
      
      await loadInvitations(true);
    } catch (error: any) {
      if (error?.status === 404 || error?.response?.status === 404) {
        try {
          await invitationsService.deleteInvitation(id);
          setMessage(t('deviceSharing.invitationRemoved'));
          
          await loadInvitations(true);
        } catch (fallbackError) {
          setError(t('deviceSharing.removeInvitationError'));
        }
      } else {
        setError(t('deviceSharing.removeInvitationError'));
      }
    }
  }, [loadInvitations, t]);

  useEffect(() => {
    if (!selectedSiteId) {
      setInvitations(null);
      loadInvitationsRef.current = false;
      loadInvitationsRefId.current = undefined;
      return;
    }

    // Load invitations when site changes
    loadInvitations(false);
  }, [selectedSiteId, loadInvitations]);

  return {
    email,
    setEmail,
    error,
    message,
    loading,
    invitations,
    sendInvitation,
    removeInvitation,
    removeUser,
    reload: () => loadInvitations(true),
  };
}
