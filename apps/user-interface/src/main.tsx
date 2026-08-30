import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { CssBaseline } from "@mui/material";
import { HashRouter, Route, Routes } from "react-router";
import { SettingsView } from "@/containers/SettingsView/SettingsView.tsx";
import { ScreensView } from "@/containers/ScreensView";
import { AppLayout } from "@components/AppLayout/AppLayout.tsx";
import { MainView } from "@/containers/MainView/MainView.tsx";
import { ScreenEditorOverlayHost } from "@/containers/ScreenEditorOverlay/ScreenEditorOverlayHost.tsx";
import { Providers } from "./providers.tsx";
import "./index.css";

createRoot(document.getElementById("root")!).render(
  <StrictMode>
    <CssBaseline />
    <Providers>
      <HashRouter>
        <Routes>
          <Route element={<AppLayout />}>
            <Route path="/" element={<MainView />} />
            <Route path="/settings" element={<SettingsView />} />
            <Route path="/settings/screens" element={<ScreensView />} />
          </Route>
        </Routes>
      </HashRouter>
      <ScreenEditorOverlayHost />
    </Providers>
  </StrictMode>,
);
