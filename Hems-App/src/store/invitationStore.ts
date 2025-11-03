import AsyncStorage from '@react-native-async-storage/async-storage';
import { create } from 'zustand';

interface InvitationState {
  pendingInvitationCode: string | null;
  isInvitationModalVisible: boolean;
  setPendingInvitationCode: (code: string | null) => Promise<void>;
  clearPendingInvitation: () => Promise<void>;
  showInvitationModal: (code: string) => void;
  hideInvitationModal: () => void;
}

const STORAGE_KEY = '@pending_invitation_code';

export const useInvitationStore = create<InvitationState>((set, get) => ({
  pendingInvitationCode: null,
  isInvitationModalVisible: false,

  setPendingInvitationCode: async (code: string | null) => {
    set({ pendingInvitationCode: code });
    if (code) {
      await AsyncStorage.setItem(STORAGE_KEY, code);
    } else {
      await AsyncStorage.removeItem(STORAGE_KEY);
    }
  },

  clearPendingInvitation: async () => {
    set({ pendingInvitationCode: null, isInvitationModalVisible: false });
    await AsyncStorage.removeItem(STORAGE_KEY);
  },

  showInvitationModal: (code: string) => {
    set({ isInvitationModalVisible: true, pendingInvitationCode: code });
  },

  hideInvitationModal: () => {
    set({ isInvitationModalVisible: false });
  },
}));

export const loadPendingInvitationFromStorage = async () => {
  try {
    const code = await AsyncStorage.getItem(STORAGE_KEY);
    if (code) {
      return code;
    }
    return null;
  } catch (error) {return null;
  }
};

