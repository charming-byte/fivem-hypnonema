import {
  type ToggleButtonProps as BaseToggleButtonProps,
  Tooltip,
} from "@mui/material";
import { StyledToggleButton } from "./StyledToggleButton.tsx";

type ToggleButtonProps = BaseToggleButtonProps & {
  tooltipTitle: string;
};

export const ToggleButton = ({
  value,
  selected,
  onChange,
  children,
  tooltipTitle,
  disabled,
  ...props
}: ToggleButtonProps) => {
  return (
    <Tooltip title={tooltipTitle} placement="top">
      <span>
        <StyledToggleButton
          size="large"
          value={value}
          selected={selected}
          onChange={onChange}
          disabled={disabled}
          {...props}
        >
          {children}
        </StyledToggleButton>
      </span>
    </Tooltip>
  );
};
