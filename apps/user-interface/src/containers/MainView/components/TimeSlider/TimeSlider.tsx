import { useCallback, useEffect, useRef, useState } from "react";
import { Stack, styled, Typography } from "@mui/material";
import { Slider } from "@/containers/MainView/components/Slider/Slider.tsx";
import { formatTime } from "@/utils/misc.ts";

const Time = styled(Typography)({
  fontSize: "13px",
  userSelect: "none",
});

interface TimeSliderProps {
  max?: number;
  value?: number;
  disabled?: boolean;
  paused?: boolean;
  onChange: (position: number) => void;
}

const INTERPOLATION_INTERVAL_MS = 250;
const SEEK_SETTLE_TIMEOUT_MS = 3000;

const SNAP_THRESHOLD_SECONDS = 1.5;
const DRIFT_CORRECTION_FACTOR = 0.25;

const TimeSlider = ({
  max = 0,
  value: initialValue = 0,
  disabled = false,
  paused = false,
  onChange = () => {},
}: TimeSliderProps) => {
  const isSeekingRef = useRef(false);
  const pendingSeekRef = useRef<null | {
    target: number;
    direction: "forward" | "backward";
    expiresAt: number;
  }>(null);
  const sliderRef = useRef<HTMLSpanElement>(null);
  const previousMaxRef = useRef(max);
  const frozenRef = useRef(false);

  const anchorRef = useRef<{ value: number; timestamp: number }>({
    value: initialValue,
    timestamp: Date.now(),
  });
  const [value, setValue] = useState<number>(initialValue);

  const readDisplayValue = useCallback(() => {
    const { value: anchorValue, timestamp } = anchorRef.current;
    if (frozenRef.current) return anchorValue;

    return anchorValue + (Date.now() - timestamp) / 1000;
  }, []);

  const setAnchor = useCallback((v: number) => {
    anchorRef.current = { value: v, timestamp: Date.now() };
    setValue(v);
  }, []);

  useEffect(() => {
    if (previousMaxRef.current !== max) {
      previousMaxRef.current = max;
      pendingSeekRef.current = null;
      isSeekingRef.current = false;
      setAnchor(initialValue);
      return;
    }

    if (isSeekingRef.current) return;

    if (pendingSeekRef.current) {
      const { target, direction, expiresAt } = pendingSeekRef.current;

      const reached =
        direction === "forward"
          ? initialValue >= target
          : initialValue <= target;

      if (!reached && Date.now() < expiresAt) return;

      pendingSeekRef.current = null;
    }

    const drift = initialValue - readDisplayValue();

    if (Math.abs(drift) > SNAP_THRESHOLD_SECONDS) {
      setAnchor(initialValue);
      return;
    }

    anchorRef.current = {
      ...anchorRef.current,
      value: anchorRef.current.value + drift * DRIFT_CORRECTION_FACTOR,
    };

    setValue(readDisplayValue());
  }, [initialValue, max, setAnchor, readDisplayValue]);

  useEffect(() => {
    if (disabled || paused) {
      anchorRef.current = { value: readDisplayValue(), timestamp: Date.now() };
      frozenRef.current = true;
      setValue(anchorRef.current.value);
      return;
    }

    frozenRef.current = false;
    anchorRef.current = { ...anchorRef.current, timestamp: Date.now() };

    const interval = setInterval(() => {
      if (isSeekingRef.current) return;

      const next = readDisplayValue();
      setValue(max > 0 ? Math.min(next, max) : next);
    }, INTERPOLATION_INTERVAL_MS);

    return () => clearInterval(interval);
  }, [disabled, paused, max, readDisplayValue]);

  return (
    <Stack direction="row" gap={3}>
      <Time variant="overline">{formatTime(value)}</Time>
      <Slider
        min={0}
        max={max}
        value={value}
        ref={sliderRef}
        disabled={disabled}
        valueLabelDisplay="auto"
        valueLabelFormat={formatTime}
        onChange={() => (isSeekingRef.current = true)}
        onChangeCommitted={(_ev, v) => {
          isSeekingRef.current = false;
          pendingSeekRef.current = {
            target: v as number,
            direction: (v as number) >= value ? "forward" : "backward",
            expiresAt: Date.now() + SEEK_SETTLE_TIMEOUT_MS,
          };
          onChange(v as number);
          sliderRef.current?.querySelector("input")?.blur();
          setAnchor(v as number);
        }}
      />
      <Time variant="overline">{formatTime(max)}</Time>
    </Stack>
  );
};
export default TimeSlider;
