import { Box, type SliderProps as MuiSliderProps, styled } from "@mui/material";
import { forwardRef, useEffect, useState } from "react";
import StyledSlider from "./StyledSlider.tsx";

const Wrapper = styled(Box)(() => ({
  "&.MuiBox-root": {
    border: "2px solid transparent !important",
    background:
      "linear-gradient(90deg, #282828 69.43%, #141414 129.87%) padding-box, linear-gradient(to top, rgb(30, 30, 30), rgb(13, 13, 13)) border-box",
    boxShadow: "0 1px 0 #525252",
    borderRadius: "18px",
    display: "flex",
    alignItems: "center",
    paddingLeft: "24px",
    paddingRight: "24px",
    position: "relative",
    width: "100%",
    boxSizing: "border-box",
    ":before": {
      content: '""',
      position: "absolute",
      top: 0,
      right: 0,
      left: 0,
      bottom: 0,
      zIndex: -1,
      margin: "-3px",
      borderRadius: "inherit",
      boxShadow: "0px -2px 19px 0px rgba(0, 0, 0, 0.7)",
      background:
        "linear-gradient(180deg, rgba(20, 20, 20, 1) 90%, rgba(102, 102, 102, 1) 100%)",
    },
  },
}));

export const Slider = forwardRef<HTMLSpanElement, MuiSliderProps>(
  ({ ...props }, ref) => {
    const [value, setValue] = useState<number>(
      props.value ? (props.value as number) : 0,
    );

    useEffect(() => {
      setValue(props.value ? (props.value as number) : 0);
    }, [props.value]);

    return (
      <Wrapper>
        <StyledSlider
          {...props}
          ref={ref}
          value={value}
          onChange={(event, newValue, activeThumb) => {
            setValue(newValue as number);
            // must still run: callers rely on onChange to know a drag is in progress
            props.onChange?.(event, newValue, activeThumb);
          }}
        />
      </Wrapper>
    );
  },
);

Slider.displayName = "Slider";
