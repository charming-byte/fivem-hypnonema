import { lazy, Suspense, useState } from "react";
import { NuiEvents } from "@hypnonema/generated-types";
import { useNuiEvent } from "@/hooks/useNuiEvent.ts";
import { isDevelopmentBrowser } from "@/utils/misc.ts";

// A second surface on the single ui_page. It's rarely open, so its module graph is a dynamic import
// (evaluated only once a session opens); vite-plugin-singlefile still inlines it into the one bundle,
// so there is no separate file on disk.
const ScreenEditorOverlay = lazy(
  () => import("@/containers/ScreenEditorOverlay/ScreenEditorOverlay.tsx"),
);

export const ScreenEditorOverlayHost = () => {
  const [open, setOpen] = useState(isDevelopmentBrowser);

  useNuiEvent(NuiEvents.editorOpen, () => setOpen(true));
  useNuiEvent(NuiEvents.editorClose, () => setOpen(false));

  if (!open) return null;

  return (
    <Suspense fallback={null}>
      <ScreenEditorOverlay />
    </Suspense>
  );
};
