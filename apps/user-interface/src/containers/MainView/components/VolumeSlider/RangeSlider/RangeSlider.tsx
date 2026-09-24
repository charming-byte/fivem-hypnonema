import { RangeSliderTrackSegment } from "./RangeSliderTrackSegment.tsx";
import type { RangeSliderProps } from "./RangeSlider.types.ts";
import { Box } from "@mui/material";

const itemWidth = 4;
const gap = itemWidth;
const width = 140;
const clamp = (val: number, min: number, max: number) =>
  Math.min(Math.max(val, min), max);

const RangeSlider = ({
  min,
  max,
  value,
  sliderRef,
  onMouseDown,
  onKeyDown,
  isDragging,
  disabled,
}: RangeSliderProps) => {
  const segmentCount = Math.floor((width + gap) / (itemWidth + gap));

  const valueItems = Math.round((clamp(value, min, max) / max) * segmentCount);

  return (
    <div
      style={{
        pointerEvents: disabled ? "none" : "auto",
        display: "flex",
        position: "relative",
        cursor: isDragging ? "grabbing" : "grab",
        height: "1.875em",
        width: `8.75em`,
      }}
      role="slider"
      tabIndex={0}
      aria-valuemin={min}
      aria-valuemax={max}
      aria-valuenow={value}
      onMouseDown={onMouseDown}
      onKeyDown={onKeyDown}
      ref={sliderRef}
    >
      <Box display="flex" gap={`0.3em`} position="absolute" width={`8.75em`}>
        {[...Array(segmentCount).keys()].map((_, i) => (
          <RangeSliderTrackSegment key={`track-bg-${i}`} width={itemWidth} />
        ))}
      </Box>

      <Box display="flex" gap={`0.3em`} width={`8.75em`}>
        {[...Array(valueItems).keys()].map((_, i) => (
          <RangeSliderTrackSegment
            key={`track-fg-${i}`}
            width={itemWidth}
            background="linear-gradient(180deg, #5F0877 0%, rgba(95, 8, 119, 0.5) 100%)"
            border="#330242"
          />
        ))}
      </Box>
    </div>
  );
};

export default RangeSlider;
