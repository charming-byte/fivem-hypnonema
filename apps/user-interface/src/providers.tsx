import { type PropsWithChildren } from "react";
import { theme } from "./utils/theme.tsx";
import { ThemeProvider } from "@mui/material";
import { SnackbarProvider } from "notistack";
import { MediaPlayersProvider } from "@/contexts/mediaPlayerContext.tsx";
import { SnackbarButton } from "@components/SnackbarButton";
import { SettingsProvider } from "@/contexts/settingsContext.tsx";
import { ScreensProvider } from "@/containers/ScreensView";

export const Providers = ({ children }: PropsWithChildren) => {
  return (
    <ThemeProvider theme={theme}>
      <SnackbarProvider
        autoHideDuration={3500}
        action={(key) => <SnackbarButton id={key} />}
        anchorOrigin={{ vertical: "bottom", horizontal: "center" }}
      >
        <SettingsProvider>
          <MediaPlayersProvider>
            <ScreensProvider>{children}</ScreensProvider>
          </MediaPlayersProvider>
        </SettingsProvider>
      </SnackbarProvider>
    </ThemeProvider>
  );
};
