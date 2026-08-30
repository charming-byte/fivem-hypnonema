import { type ReactNode, useState } from "react";
import { Box, Stack, Typography } from "@mui/material";
import type {
  ScreenEditorOverlayState,
  Vector3,
} from "@hypnonema/generated-types";
import { NuiEvents } from "@hypnonema/generated-types";
import { useNuiEvent } from "@/hooks/useNuiEvent.ts";
import { isDevelopmentBrowser } from "@/utils/misc.ts";
import { EditorFinishDialog } from "./EditorFinishDialog.tsx";
import { persistLegendPref, readLegendPref } from "./legendPref.ts";

const MUTED = "rgba(255,255,255,0.5)";
const MUTED_SOFT = "rgba(255,255,255,0.6)";
const PANEL_BG = "rgba(10,10,12,0.55)";

// Matches the native gizmo's per-axis colours in ScreenEditorSession.AxisColor.
const AXIS_COLOR: Record<string, string> = {
  X: "rgb(235, 64, 52)",
  Y: "rgb(66, 214, 92)",
  Z: "rgb(74, 136, 255)",
};

// Only reachable in a dev browser (see ScreenEditorOverlayHost) - a static sample so the overlay can
// be styled without the game running.
const DEV_STATE: ScreenEditorOverlayState = {
  position: { x: 1234.56, y: -784.21, z: 41.03 },
  rotation: { x: 0, y: 0, z: 125 },
  scale: { x: 1, y: 0.5625 },
  mode: "Translate",
  frame: "Local",
  axis: "X",
  aspectLocked: true,
  dirty: false,
};

const LEGEND_LINES = [
  ["F1", "toggle this legend"],
  ["1 / 2 / 3", "translate / rotate / scale"],
  ["X / Y / Z", "select the active axis"],
  ["hold LMB on a handle", "drag along that axis (camera freezes)"],
  ["arrows", "nudge active axis (0.05 m / 5°)"],
  ["Shift ×10", "Alt ×0.1 (Alt also frees the rotation grid)"],
  ["G", "toggle local / world frame"],
  ["L", "toggle the scale aspect lock"],
  ["E", "snap the face onto the surface under the crosshair"],
  ["Enter", "name and save this screen"],
  ["Escape", "close the editor"],
];

const NUM_WIDTH = 9; // right-align each component in a fixed column (sign + 4 int + dot + 2 frac + slack)

const Vec = ({ value, digits }: { value: Vector3; digits: number }) => (
  <Box component="span" sx={{ fontVariantNumeric: "tabular-nums" }}>
    {[value.x, value.y, value.z]
      .map((n) => n.toFixed(digits).padStart(NUM_WIDTH))
      .join("  ")}
  </Box>
);

const Row = ({ label, children }: { label: string; children: ReactNode }) => (
  <Stack direction="row" spacing={1.5}>
    <Box component="span" sx={{ width: "3.5rem", color: MUTED }}>
      {label}
    </Box>
    {children}
  </Stack>
);

const Field = ({ label, children }: { label: string; children: ReactNode }) => (
  <span>
    <Box component="span" sx={{ color: MUTED }}>
      {label}{" "}
    </Box>
    {children}
  </span>
);

export default function ScreenEditorOverlay() {
  const [state, setState] = useState<ScreenEditorOverlayState | null>(() =>
    isDevelopmentBrowser() ? DEV_STATE : null,
  );
  const [legendVisible, setLegendVisible] = useState(readLegendPref);

  useNuiEvent<ScreenEditorOverlayState>(NuiEvents.editorState, setState);
  useNuiEvent(NuiEvents.editorToggleLegend, () => {
    const next = !legendVisible;
    persistLegendPref(next);
    setLegendVisible(next);
  });

  return (
    <>
      <EditorFinishDialog />
      {state && <ValueOverlay state={state} legendVisible={legendVisible} />}
    </>
  );
}

function ValueOverlay({
  state,
  legendVisible,
}: {
  state: ScreenEditorOverlayState;
  legendVisible: boolean;
}) {
  return (
    <Box
      sx={{
        position: "fixed",
        inset: 0,
        pointerEvents: "none",
        fontFamily: "ui-monospace, 'Roboto Mono', monospace",
        fontSize: "0.8rem",
        lineHeight: 1.7,
        color: "rgba(255,255,255,0.92)",
        textShadow: "0 1px 2px rgba(0,0,0,0.9)",
        userSelect: "none",
      }}
    >
      <Box
        sx={{
          position: "absolute",
          top: "6rem",
          right: "3rem",
          px: 2,
          py: 1.5,
          borderRadius: 1,
          bgcolor: PANEL_BG,
          backdropFilter: "blur(2px)",
        }}
      >
        <Stack direction="row" spacing={1} alignItems="center" sx={{ mb: 0.5 }}>
          <Typography
            component="span"
            sx={{ fontSize: "0.72rem", letterSpacing: "0.18em", fontWeight: 700 }}
          >
            SCREEN EDITOR
          </Typography>
          {state.dirty && (
            <Box
              component="span"
              sx={{ fontSize: "0.68rem", color: "rgb(255, 196, 84)" }}
            >
              ● unsaved
            </Box>
          )}
        </Stack>

        <Stack direction="row" spacing={2} sx={{ mb: 0.5 }}>
          <Field label="mode">{state.mode}</Field>
          <Field label="frame">{state.frame}</Field>
          <Field label="axis">
            <Box
              component="span"
              sx={{ color: AXIS_COLOR[state.axis], fontWeight: 700 }}
            >
              {state.axis}
            </Box>
          </Field>
        </Stack>

        <Row label="pos">
          <Vec value={state.position} digits={2} />
        </Row>
        <Row label="rot">
          <Vec value={state.rotation} digits={2} />
        </Row>
        <Row label="scale">
          <Box component="span" sx={{ fontVariantNumeric: "tabular-nums" }}>
            {state.scale.x.toFixed(3)} / {state.scale.y.toFixed(3)}
            {state.aspectLocked && (
              <Box component="span" sx={{ color: MUTED }}>
                {"  (aspect locked)"}
              </Box>
            )}
          </Box>
        </Row>
      </Box>

      {legendVisible && (
        <Box
          sx={{
            position: "absolute",
            bottom: "3rem",
            right: "3rem",
            px: 2,
            py: 1.5,
            borderRadius: 1,
            bgcolor: PANEL_BG,
            backdropFilter: "blur(2px)",
          }}
        >
          {LEGEND_LINES.map(([keys, description]) => (
            <Stack key={keys} direction="row" spacing={1.5}>
              <Box
                component="span"
                sx={{ minWidth: "10rem", fontWeight: 600 }}
              >
                {keys}
              </Box>
              <Box component="span" sx={{ color: MUTED_SOFT }}>
                {description}
              </Box>
            </Stack>
          ))}
        </Box>
      )}
    </Box>
  );
}
