import {
  type BaseSettingItemProps,
  SettingListItemBase,
} from "@/containers/SettingsView/components/SettingsList/SettingsListItemBase.tsx";
import { Box, Typography } from "@mui/material";
import Switch from "@components/Switch";

export interface SettingListItemSwitchProps
  extends Omit<BaseSettingItemProps, "children"> {
  value: boolean;
  onToggle?: (checked: boolean) => void;
  disabled?: boolean;
  leftLabel?: string;
  rightLabel?: string;
}

export const SettingListItemSwitch = ({
  title,
  helperText,
  value,
  onToggle,
  disabled,
  leftLabel = "Off",
  rightLabel = "On",
}: SettingListItemSwitchProps) => (
  <SettingListItemBase title={title} helperText={helperText}>
    <Box display="flex" alignItems="center">
      <Typography variant="body2" sx={{ mr: 1 }}>
        {leftLabel}
      </Typography>
      <Switch
        label={""}
        checked={value}
        onChange={(e) => onToggle?.(e)}
        disabled={disabled}
      />
      <Typography variant="body2" sx={{ ml: 1 }}>
        {rightLabel}
      </Typography>
    </Box>
  </SettingListItemBase>
);
