import {
  type BaseSettingItemProps,
  SettingListItemBase,
} from "@/containers/SettingsView/components/SettingsList/SettingsListItemBase.tsx";
import { Button } from "@mui/material";

export interface SettingListItemButtonProps
  extends Omit<BaseSettingItemProps, "children"> {
  buttonText?: string;
  onClick?: () => void;
  disabled?: boolean;
}

export const SettingListItemButton = ({
  title,
  helperText,
  buttonText,
  onClick,
  disabled,
}: SettingListItemButtonProps) => (
  <SettingListItemBase title={title} helperText={helperText}>
    <Button
      variant="text"
      color="inherit"
      onClick={onClick}
      disabled={disabled}
    >
      {buttonText ?? title.toUpperCase()}
    </Button>
  </SettingListItemBase>
);
