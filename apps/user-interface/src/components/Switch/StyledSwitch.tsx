import { styled, Switch, type SwitchProps } from "@mui/material";

export const StyledSwitch = styled((props: SwitchProps) => (
  <Switch focusVisibleClassName=".Mui-focusVisible" disableRipple {...props} />
))(({ theme }) => ({
  width: 67,
  height: 31,
  padding: 0,
  boxSizing: "border-box",
  background: "linear-gradient(0deg, #282828 0%, #141414 100%)",
  borderRadius: "18px",

  boxShadow:
    "1px 0px 0px 0px #1E1E1E,inset 0px -1px 0px 0px rgba(255,255,255,0.2), inset 0px -4px 0px 0px #1E1E1E, inset 0px 2px 0px rgba(0,0,0,0.7),0px 0px 0px 0px rgb(0 0 0),inset 0px 0px 0px 0px rgb(0 0 0),  0px 0px 0px 0px rgb(0 0 0)",
  position: "relative",

  "& .MuiSwitch-root": {},
  "& .MuiSwitch-switchBase": {
    padding: 0,
    margin: "6px",
    transitionDuration: "250ms",
    "&.Mui-checked": {
      transform: "translateX(32px)",
      color: "#fff",
      "& + .MuiSwitch-track": {
        backgroundColor: "#5F0877",
        boxSizing: "border-box",
        opacity: 1,
        boxShadow:
          " 0px 1px 0px 0px rgba(255,255,255,0.3),inset 1px 0px 0px 0px rgb(0 0 0),inset 0px 1px 0px 0px rgb(0 0 0)",
      },
      "&.Mui-disabled + .MuiSwitch-track": {
        opacity: 0.5,
      },
    },
    "&.Mui-focusVisible .MuiSwitch-thumb": {
      color: theme.palette.primary.main,
      borderWidth: "2px",
    },
    "&.Mui-disabled .MuiSwitch-thumb": {
      color:
        theme.palette.mode === "light"
          ? theme.palette.grey[100]
          : theme.palette.grey[600],
    },
    "&.Mui-disabled + .MuiSwitch-track": {
      opacity: theme.palette.mode === "light" ? 0.7 : 0.3,
    },
  },
  "& .MuiSwitch-thumb": {
    boxSizing: "border-box",
    background: "linear-gradient(180deg, #3C3C3C 0%, #1E1E1E 100%)",
    width: 19,
    height: 19,

    ":before": {
      content: '""',
      position: "absolute",
      top: 0,
      right: 0,
      left: 0,
      bottom: 0,
      zIndex: -1,
      margin: "-1px",
      borderRadius: "inherit",
      background:
        "linear-gradient(to bottom, #646464 4%, rgba(0, 0, 0, 1) 100%)",
    },
  },
  "& .MuiSwitch-switchBase.Mui-checked + .MuiSwitch-track": {
    boxShadow:
      "inset 1px 0px 0px 0px rgb(0 0 0),inset 0px -1px 0px 0px rgb(0 0 0), inset 0px 1px 0px 0px rgb(0 0 0) !important",
  },
  "& .MuiSwitch-track": {
    position: "absolute",
    marginLeft: "auto",
    marginRight: "auto",
    marginTop: "auto",
    marginBottom: "auto",

    top: 0,
    bottom: 0,
    left: 0,
    right: 0,
    borderRadius: 26 / 2,
    height: 16,
    width: 48,
    backgroundColor: "#000000",
    opacity: 1,
    transition: theme.transitions.create(["background-color"], {
      duration: 250,
    }),
  },
}));
