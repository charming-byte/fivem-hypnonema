import { styled, ToggleButton } from "@mui/material";
import type { ToggleButtonProps } from "@mui/material";

export const StyledToggleButton = styled(ToggleButton)<ToggleButtonProps>(
  () => ({
    "&.MuiToggleButton-root": {
      background: "linear-gradient(180deg, #323232 0%, #171717 100%)",
      boxSizing: "border-box",
      position: "relative",
      outline: "black solid 2px",
      backgroundClip: "padding-box",
      border: "solid 1px transparent",

      "&:hover": {
        background: "linear-gradient(0deg, #171717 0%, #323232 100%)",
      },
    },

    "&.Mui-disabled": {
      color: "#646464 !important",
    },

    "&.Mui-selected.Mui-disabled": {
      background: "linear-gradient(180deg, #323232 0%, #171717 100%)",
      color: "#646464 !important",
      outline: "black solid 2px",
      boxShadow: "none",
      border: "solid 1px transparent",
    },

    "&.Mui-selected": {
      background: "linear-gradient(0deg, #171717 0%, #323232 100%)",
      color: "#7D0B9C !important",
      outline: "black solid 2px",
      boxShadow: "0px 2px 0 1px rgba(255, 255, 255, 0.1)",
      border: "solid 1px transparent",

      "&:before": {
        background: "none",
      },

      "&:hover": {
        background: "linear-gradient(0deg, #323232  0%, #171717 100%)",
      },
    },

    "&.MuiButtonBase-root": {
      color: "rgba(255,255,255,0.8)",

      "&:hover": {
        color: "#7D0B9C",
      },
    },
  }),
);
