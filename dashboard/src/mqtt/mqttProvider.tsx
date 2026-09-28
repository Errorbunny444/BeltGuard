import React, { createContext, useContext } from 'react';
import { useMQTT } from './hooks/useMQTT';
import { MQTTConnectionStatus } from './types/mqtt';

interface MQTTContextValue {
  status: MQTTConnectionStatus;
}

const MQTTContext = createContext<MQTTContextValue>({
  status: 'DISCONNECTED',
});

export const MQTTProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { status } = useMQTT();

  return (
    <MQTTContext.Provider value={{ status }}>
      {children}
    </MQTTContext.Provider>
  );
};

export const useMQTTStatus = () => useContext(MQTTContext).status;
