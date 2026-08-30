import { styled } from "@mui/material";
import { KnobHeadless } from "react-knob-headless";
import { StyledComponent } from "@emotion/styled";
import { ComponentProps } from "react";

type StyledKnobHeadlessProps = ComponentProps<typeof KnobHeadless>;

export const StyledKnobHeadless: StyledComponent<StyledKnobHeadlessProps> =
  styled(KnobHeadless)({
    background: "linear-gradient(180deg, #282828 0%, #0A0A0A 100%)",
    borderRadius: "40px",
    width: "79px",
    height: "79px",
    display: "flex",
    justifyContent: "center",
    alignItems: "center",
    boxShadow: "inset 0px 2px 0px rgba(0,0,0,1),inset 0px -1px 0px #585858f7",
    position: "relative",
    outline: "2px solid transparent",
  });
