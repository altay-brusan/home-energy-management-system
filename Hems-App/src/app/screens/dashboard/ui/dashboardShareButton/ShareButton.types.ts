export type FontAwesomeName =
  | 'bolt'
  | 'battery-full'
  | 'plug'
  | 'link'
  | 'whatsapp'
  | 'twitter'
  | 'envelope'
  | 'share';

export type ShareButtonProps = {
  color: string;
  backgroundColor: string;
  top?: number;
  right?: number;
  COLORS: Record<string, string>;
};

export type ShareOption = {
  label: string;
  icon: FontAwesomeName;
  action: () => void | Promise<void>;
};
