import { MenuItem, Stack, styled, TextField, Tooltip } from "@mui/material";

const StyledTextField = styled(TextField)({
  flexShrink: 1,
  width: "100%",
  boxShadow: "-2px -2px 0px #101010, 2px 0px 0px #101010",
  borderRadius: "4px",
  "& .MuiSelect-select": {
    height: "16px",
    minHeight: "16px",
    backgroundColor: "transparent",
  },
  "& .MuiInputBase-root": {
    boxShadow: " 0px 1px 0px rgba(88, 88, 88, 0.4)",
    borderRadius: "4px",
    ":before": {
      borderBottom: "1px solid rgba(88, 88, 88,0.67)",
    },
  },
  "& .MuiFilledInput-root": {
    backgroundColor: "transparent",
    ":hover": {
      backgroundColor: "transparent",
    },
  },
});
interface SelectItem {
  label: string;
  value: string | number;
  description?: string;
}

interface SelectProps {
  value?: string | readonly string[] | number | undefined;
  onChange?: (value: string | number) => void;
  items?: SelectItem[];
  disabled?: boolean;
  label?: string;
  tooltipTitle?: string;
  width?: string;
}

export const Select = ({
  onChange = () => {},
  value,
  disabled = false,
  label = "",
  items,
  tooltipTitle = "",
  width = "12rem",
}: SelectProps) => {
  return (
    <Stack direction="row" gap={2}>
      <Tooltip title={tooltipTitle} placement="top">
        <span>
          <StyledTextField
            select
            label={label}
            variant="filled"
            value={value}
            onChange={(event) => onChange(event.target.value)}
            disabled={disabled}
            sx={{ width }}
          >
            {items?.map((item) => (
              <MenuItem
                value={item.value}
                key={item.label}
                selected={value == item.value}
              >
                {item.description && !disabled ? (
                  <Tooltip title={item.description} placement="right">
                    <span>{item.label}</span>
                  </Tooltip>
                ) : (
                  item.label
                )}
              </MenuItem>
            ))}
          </StyledTextField>
        </span>
      </Tooltip>
    </Stack>
  );
};
