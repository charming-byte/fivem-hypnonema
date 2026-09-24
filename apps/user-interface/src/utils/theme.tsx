import { createTheme } from "@mui/material";

declare module "@mui/material/styles" {
  interface Theme {
    colors: {
      backgroundGradient: string;
    };
  }

  interface ThemeOptions {
    colors?: {
      backgroundGradient?: string;
    };
  }
}

export const theme = createTheme({
  palette: {
    mode: "dark",
    primary: {
      main: "#5F0877",
    },
  },
  colors: {
    backgroundGradient:
      "radial-gradient(87.34% 121.2% at 54.36% 52.95%, #323232 0%, #141414 100%)",
  },
  components: {
    MuiDivider: {
      styleOverrides: {
        textAlignLeft: {
          borderColor: "#0c0c0c",
          "::before": {
            boxShadow: "0 -1px 0 rgba(0,0,0,0.7),0 0 0 rgba(219,219,219,0.5)",
          },
          "::after": {
            boxShadow: "0 -1px 0 rgba(0,0,0,0.7),0 0 0 rgba(219,219,219,0.5)",
          },
        },
        vertical: {
          borderColor: "rgba(0,0,0,0.4)",
          boxShadow:
            "1px 0px 0 0 rgba(82,82,82,0.4), 1px 1px 0 0 rgba(82,82,82,0.2)",
        },
      },
    },
  },
});
