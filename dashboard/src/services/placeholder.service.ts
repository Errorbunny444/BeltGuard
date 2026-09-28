export interface ModalInfo {
  isOpen: boolean;
  title: string;
  description: string;
  details?: string;
  badge?: string;
}

type ModalListener = (modal: ModalInfo) => void;

class PlaceholderService {
  private modal: ModalInfo = {
    isOpen: false,
    title: '',
    description: '',
  };
  private listeners: Set<ModalListener> = new Set();

  public subscribe(listener: ModalListener): () => void {
    this.listeners.add(listener);
    listener(this.modal);
    return () => {
      this.listeners.delete(listener);
    };
  }

  public showFeatureNotice(title: string, description: string, details?: string) {
    this.modal = {
      isOpen: true,
      title,
      description,
      details,
      badge: 'Pending Hardware / Backend Integration',
    };
    this.notify();
  }

  public closeModal() {
    this.modal = {
      isOpen: false,
      title: '',
      description: '',
    };
    this.notify();
  }

  private notify() {
    this.listeners.forEach((listener) => listener({ ...this.modal }));
  }
}

export const placeholderService = new PlaceholderService();
