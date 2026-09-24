import {
  type KeyboardEvent,
  type MouseEvent,
  useCallback,
  useEffect,
  useRef,
  useState,
} from "react";
import type { RangeSliderContainerProps } from "./RangeSlider.types.ts";
import RangeSlider from "./RangeSlider.tsx";

const RangeSliderContainer = ({
  initialValue = 100,
  onChange = () => {},
  step = 5,
  min,
  max,
  disabled,
}: RangeSliderContainerProps) => {
  const sliderRef = useRef<HTMLDivElement | null>(null);
  const [value, setValue] = useState(initialValue);
  const [isDragging, setIsDragging] = useState(false);

  useEffect(() => {
    setValue(initialValue);
  }, [initialValue]);

  const moveSliderPosition = useCallback(
    (event: MouseEvent) => {
      const sliderBoundingClientRect =
        sliderRef.current?.getBoundingClientRect();

      if (sliderBoundingClientRect) {
        const posX =
          (event as MouseEvent).clientX - sliderBoundingClientRect.left;
        const totalWidth = sliderBoundingClientRect.width;

        let selectedValue = Math.round((posX / totalWidth) * (max - min) + min);
        selectedValue = Math.max(min, selectedValue);
        selectedValue = Math.min(max, selectedValue);

        setValue(selectedValue);
      }
    },
    [max, min],
  );

  const onMouseUp = useCallback(() => {
    onChange(value);
    setIsDragging(false);
  }, [value, onChange]);

  const onMouseMove = useCallback(
    (event: Event) => {
      if (isDragging) {
        moveSliderPosition(event as unknown as MouseEvent);
      }
    },
    [isDragging, moveSliderPosition],
  );

  const onMouseDown = (event: MouseEvent) => {
    moveSliderPosition(event);
    setIsDragging(true);
  };

  const onKeyDown = (event: KeyboardEvent) => {
    if (event.key === "ArrowLeft" || event.key === "ArrowRight") {
      let selectedValue = value;

      if (event.key === "ArrowLeft") {
        selectedValue = Math.max(value - step, min);
      } else if (event.key === "ArrowRight") {
        selectedValue = Math.min(value + step, max);
      }

      setValue(selectedValue);
      onChange(selectedValue);
    }
  };

  useEffect(() => {
    if (isDragging) {
      window.addEventListener("mousemove", onMouseMove);
      window.addEventListener("mouseup", onMouseUp);
    }

    return () => {
      window.removeEventListener("mousemove", onMouseMove);
      window.removeEventListener("mouseup", onMouseUp);
    };
  }, [isDragging, onMouseMove, onMouseUp]);

  return (
    <RangeSlider
      min={min}
      max={max}
      value={disabled ? 0 : value}
      sliderRef={sliderRef}
      onMouseDown={onMouseDown}
      onKeyDown={onKeyDown}
      isDragging={isDragging}
      disabled={disabled}
    />
  );
};

export default RangeSliderContainer;
