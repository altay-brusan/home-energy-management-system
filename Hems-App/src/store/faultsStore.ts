import create from 'zustand';

type FaultStatus = 'in_progress' | 'processing' | 'completed';

export type Fault = {
  id: string;
  faultType: string;
  description: string;
  name: string;
  phone: string;
  address: string;
  zip: string;
  email: string;
  status: FaultStatus;
  createdAt: string;
  finishedAt?: string;
  technicianNote?: string;
};

export type FaultsState = {
  faults: Fault[];
  addFault: (fault: Omit<Fault, 'id' | 'status' | 'createdAt'>) => void;
};

export const useFaultsStore = create<FaultsState>((set) => ({
  faults: [],
  addFault: (fault) =>
    set((state) => ({
      faults: [
        ...state.faults,
        {
          ...fault,
          id: Date.now().toString(),
          status: 'in_progress',
          createdAt: new Date().toISOString(),
        },
      ],
    })),
}));
