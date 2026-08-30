import { ComponentProps, CSSProperties, useId, useMemo, useState } from "react";
import {
  KnobHeadless,
  KnobHeadlessOutput,
  useKnobKeyboardControls,
} from "react-knob-headless";
import { Box, Stack, Typography } from "@mui/material";
import { KnobBaseThumb } from "./KnobBaseThumb.tsx";
import debounce from "lodash.debounce";
import { StyledKnobHeadless } from "./StyledKnobHeadless.tsx";
import InfoTooltipIcon from "@components/InfoTooltipIcon/index.tsx";

type KnobHeadlessProps = ComponentProps<typeof KnobHeadless> & {
  disabled?: boolean;
};

type KnobBaseProps = Pick<
  KnobHeadlessProps,
  | "valueMax"
  | "valueMin"
  | "valueRawRoundFn"
  | "valueRawDisplayFn"
  | "orientation"
  | "mapTo01"
  | "mapFrom01"
> & {
  readonly label: string;
  readonly valueDefault: number;
  readonly stepFn: (valueRaw: number) => number;
  readonly stepLargerFn: (valueRaw: number) => number;
  readonly style?: CSSProperties;
  readonly handleChange?: (valueRaw: number) => void;
  disabled?: boolean;
  tooltip?: string;
};

const DRAG_SENSITIVITY = 0.006;
const DEBOUNCE_DELAY = 300;

export const KnobBase = ({
  label,
  valueDefault,
  valueMin,
  valueMax,
  valueRawRoundFn,
  valueRawDisplayFn,
  orientation,
  mapFrom01,
  stepFn,
  stepLargerFn,
  mapTo01,
  style,
  handleChange,
  tooltip,
}: KnobBaseProps) => {
  const knobId = useId();
  const labelId = useId();
  const [valueRaw, setValueRaw] = useState<number>(valueDefault);

  const value01 = useMemo(
    () => mapTo01?.(valueRaw, valueMin, valueMax),
    [valueRaw, valueMin, valueMax, mapTo01],
  );
  const step = useMemo(() => stepFn(valueRaw), [valueRaw, stepFn]);
  const stepLarger = useMemo(
    () => stepLargerFn(valueRaw),
    [valueRaw, stepLargerFn],
  );

  const debouncedChangeHandler = useMemo(
    () => (handleChange ? debounce(handleChange, DEBOUNCE_DELAY) : undefined),
    [handleChange],
  );

  const handleValueRawChange = (newValueRaw: number) => {
    setValueRaw(newValueRaw);
    if (debouncedChangeHandler) {
      debouncedChangeHandler(newValueRaw);
    }
  };

  const keyboardControlHandlers = useKnobKeyboardControls({
    valueRaw,
    valueMin,
    valueMax,
    step,
    stepLarger,
    onValueRawChange: setValueRaw,
  });

  return (
    <Stack direction="row" spacing={1}>
      <Box
        display="flex"
        flexDirection="column"
        alignItems="center"
        justifyContent="center"
        sx={{ gap: ".125rem" }}
        style={style}
      >
        <Typography sx={{ userSelect: "none" }} variant="caption">
          {label}
        </Typography>
        <StyledKnobHeadless
          id={knobId}
          aria-labelledby={labelId}
          valueMin={valueMin}
          valueMax={valueMax}
          valueRaw={valueRaw}
          valueRawRoundFn={valueRawRoundFn}
          valueRawDisplayFn={valueRawDisplayFn}
          dragSensitivity={DRAG_SENSITIVITY}
          orientation={orientation}
          mapTo01={mapTo01}
          mapFrom01={mapFrom01}
          onValueRawChange={handleValueRawChange}
          {...keyboardControlHandlers}
        >
          <KnobBaseThumb value01={value01} />
        </StyledKnobHeadless>
        <KnobHeadlessOutput htmlFor={knobId}>
          <Typography sx={{ marginTop: "6px" }}>
            {valueRawDisplayFn(valueRaw)}
          </Typography>
        </KnobHeadlessOutput>
      </Box>
      <InfoTooltipIcon tooltip={tooltip} />
    </Stack>
  );
};
