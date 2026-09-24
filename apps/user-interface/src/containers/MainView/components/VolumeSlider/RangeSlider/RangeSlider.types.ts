import type { KeyboardEvent, MouseEvent, RefObject } from "react";

export type SharedRangeSliderProps = {
  min: number;
  max: number;
  disabled?: boolean;
};

export type RangeSliderProps = SharedRangeSliderProps & {
  value: number;
  sliderRef: RefObject<HTMLDivElement | null>;
  onMouseDown: (event: MouseEvent) => void;
  onKeyDown: (event: KeyboardEvent) => void;
  isDragging: boolean;
};

export type RangeSliderContainerProps = SharedRangeSliderProps & {
  initialValue?: number;
  onChange?: (value: number) => void;
  step?: number;
};
