import React, { useState, useEffect } from 'react';
import { Header } from '@/components/common/Header';
import { Sidebar } from '@/components/common/Sidebar';
import { Footer } from '@/components/common/Footer';
import { placeholderService, ModalInfo } from '@/services/placeholder.service';
import { Info, X } from 'lucide-react';
import { motion, AnimatePresence } from 'framer-motion';

interface MainLayoutProps {
  children: React.ReactNode;
  currentTab?: string;
  onTabChange?: (tab: string) => void;
  operationalStatus?: string;
  statusMessage?: string;
}

export const MainLayout: React.FC<MainLayoutProps> = ({
  children,
  currentTab: propCurrentTab,
  onTabChange: propOnTabChange,
  operationalStatus,
  statusMessage,
}) => {
  const [internalTab, setInternalTab] = useState('Home');
  const currentTab = propCurrentTab || internalTab;
  const onTabChange = propOnTabChange || setInternalTab;

  const [modal, setModal] = useState<ModalInfo>({
    isOpen: false,
    title: '',
    description: '',
  });

  useEffect(() => {
    return placeholderService.subscribe(setModal);
  }, []);

  return (
    <div className="flex flex-col h-screen w-screen bg-[#181d27] text-slate-100 overflow-hidden font-sans">
      {/* Top Header */}
      <Header operationalStatus={operationalStatus} statusMessage={statusMessage} />

      {/* Main Body (Sidebar + Content) */}
      <div className="flex flex-1 min-h-0 overflow-hidden">
        <Sidebar currentTab={currentTab} onTabChange={onTabChange} />

        <main className="flex-1 min-w-0 p-3 flex flex-col overflow-hidden bg-[#141822]/40">
          {children}
        </main>
      </div>

      {/* Bottom Footer */}
      <Footer />

      {/* Clean Themed Control-Room Notice Modal */}
      <AnimatePresence>
        {modal.isOpen && (
          <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
            <motion.div
              initial={{ opacity: 0, scale: 0.95, y: 10 }}
              animate={{ opacity: 1, scale: 1, y: 0 }}
              exit={{ opacity: 0, scale: 0.95, y: 10 }}
              transition={{ duration: 0.15 }}
              className="w-full max-w-md bg-industrial-surface border border-industrial-border rounded-2xl p-5 shadow-2xl relative"
            >
              {/* Header */}
              <div className="flex items-start justify-between mb-3">
                <div className="flex items-center space-x-2.5">
                  <div className="w-8 h-8 rounded-lg bg-emerald-500/10 border border-emerald-500/20 flex items-center justify-center text-emerald-400">
                    <Info className="w-4 h-4" />
                  </div>
                  <div>
                    <h3 className="text-sm font-bold text-white tracking-wide">
                      {modal.title}
                    </h3>
                    {modal.badge && (
                      <span className="text-[10px] font-semibold text-emerald-400 bg-emerald-950/40 px-2 py-0.5 rounded-full border border-emerald-800/40">
                        {modal.badge}
                      </span>
                    )}
                  </div>
                </div>

                <button
                  onClick={() => placeholderService.closeModal()}
                  className="w-7 h-7 rounded-lg bg-slate-800/60 hover:bg-slate-700/80 text-slate-400 hover:text-white flex items-center justify-center transition-colors"
                >
                  <X className="w-4 h-4" />
                </button>
              </div>

              {/* Body */}
              <div className="my-3 text-xs text-slate-300 leading-relaxed space-y-2">
                <p>{modal.description}</p>
                {modal.details && (
                  <p className="text-[11px] text-slate-400 bg-industrial-subsurface/60 p-2.5 rounded-lg border border-industrial-border/60">
                    {modal.details}
                  </p>
                )}
              </div>

              {/* Footer */}
              <div className="mt-4 flex justify-end">
                <button
                  onClick={() => placeholderService.closeModal()}
                  className="px-4 py-1.5 rounded-lg text-xs font-semibold bg-slate-800 hover:bg-slate-700 text-white border border-slate-700/80 transition-colors"
                >
                  Acknowledge
                </button>
              </div>
            </motion.div>
          </div>
        )}
      </AnimatePresence>
    </div>
  );
};
