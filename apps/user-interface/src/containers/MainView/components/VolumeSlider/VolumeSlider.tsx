import RangeSliderContainer from "./RangeSlider/RangeSliderContainer.tsx";
import { VolumeOff, VolumeUp } from "@mui/icons-material";
import { Stack } from "@mui/material";
import type { SharedRangeSliderProps } from "./RangeSlider/RangeSlider.types.ts";

type VolumeSliderProps = SharedRangeSliderProps & {
  value?: number;
  onChange: (value: number) => void;
  step?: number;
  disabled?: boolean;
};

const VolumeSlider = ({
  min = 0,
  max = 100,
  value,
  onChange,
  step,
  disabled = false,
}: VolumeSliderProps) => {
  return (
    <Stack
      direction="row"
      gap={1}
      display="flex"
      alignItems="center"
      justifyContent="flex-end"
    >
      {value === 0 ? (
        <VolumeOff sx={{ width: "22px", height: "22px" }} />
      ) : (
        <VolumeUp sx={{ width: "22px", height: "22px" }} />
      )}
      <RangeSliderContainer
        min={min}
        max={max}
        initialValue={value || 0}
        onChange={onChange}
        step={step}
        disabled={disabled}
      />
    </Stack>
  );
};
export default VolumeSlider;
