import { FormControlLabel, Stack, styled } from "@mui/material";
import { StyledSwitch } from "./StyledSwitch.tsx";

const ControlLabel = styled(FormControlLabel)({
  gap: 2,
  marginLeft: "0px",
});

interface SwitchProps {
  onChange?: (checked: boolean) => void;

  checked?: boolean;
  disabled?: boolean;
  label: string;
}

const Switch = ({
  onChange = () => {},
  checked = false,
  disabled = false,
  label,
}: SwitchProps) => {
  return (
    <Stack direction="row">
      <ControlLabel
        style={{ marginRight: 0 }}
        control={
          <StyledSwitch
            checked={checked}
            disabled={disabled}
            onChange={(_ev, checked) => onChange(checked)}
          />
        }
        label={label}
      />
    </Stack>
  );
};

export default Switch;
