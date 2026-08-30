import { Slider as MuiSlider, styled } from "@mui/material";

const StyledSlider = styled(MuiSlider)(() => ({
  "& .MuiSlider-root": {
    boxSizing: "border-box",
  },

  "& .MuiSlider-track": {
    backgroundColor: "#48045a",
    filter: "blur(1px)",
    height: "9px",
    border: "none",
    borderRadius: "32px",
    mixBlendMode: "normal",
    position: "relative",
    ":after": {
      content: '""',
      position: "absolute",
      top: 0,
      right: 0,
      left: 0,
      bottom: 0,
      zIndex: -1,
      margin: "0px",
      borderRadius: "inherit",
      background: "#48045a",
    },
  },

  "& .Mui-active": {
    boxShadow: " 0px 0px 4px 2px rgba(0, 93, 129, 0.1)!important",
  },

  "& .MuiSlider-thumb": {
    backgroundColor: "rgb(53,53,53)",
    border: "1px solid  rgba(102, 102, 102, 0.7) ",
    borderRadius: "12px",
    width: "21px",
    height: "21px",
    ":hover": {
      boxShadow: "none",
    },
  },

  "& .MuiSlider-rail": {
    backgroundColor: "#000000",
    height: "12px",
    boxShadow: "0px 2px 0px rgba(255, 255, 255, 0.25)",
    borderRadius: "48px",
  },
}));

export default StyledSlider;
