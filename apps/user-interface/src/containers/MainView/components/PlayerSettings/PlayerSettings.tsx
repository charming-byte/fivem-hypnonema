import { Popover, Stack, SvgIcon } from "@mui/material";
import { Videocam, VolumeOff } from "@mui/icons-material";
import ScaleformIcon from "@/assets/scaleform.svg";
import Zoom from "@mui/material/Zoom";
import { useState } from "react";

import {
  RenderMode,
  type ScaleformRenderConfiguration as Scaleform,
  TargetType,
} from "@hypnonema/generated-types";
import { Accordion } from "@components/Accordion";
import { Select } from "@components/Select/Select.tsx";
import { useMediaPlayers } from "@/hooks/useMediaPlayers.ts";
import { ToggleButton } from "@components/ToggleButton/index.tsx";
import { ScaleformSettings } from "@/containers/MainView/components/ScaleformSettings/ScaleformSettings.tsx";

const popoverStyles = {
  background: "linear-gradient(89.96deg, #2C2B2B 38.71%, #212121 99.96%)",
  boxSizing: "border-box",
  borderRadius: "12px",
  display: "flex",
};

const renderModes = [
  {
    value: RenderMode.RenderTarget,
    label: "Model Texture (Direct Draw)",
    description:
      "Draws directly onto a custom prop model's built-in screen texture.",
  },
  {
    value: RenderMode.ScaleformRenderTarget,
    label: "Model Texture (Scaleform Draw)",
    description:
      "Draws a Scaleform onto a custom prop model's built-in screen texture, instead of drawing directly.",
  },
  {
    value: RenderMode.Scaleform,
    label: "Floating 3D Display",
    description:
      "Displays freely positioned in 3D space, no special model required.",
  },
];

export const PlayerSettings = () => {
  const { selectedMediaPlayer } = useMediaPlayers();
  const [anchorEl, setAnchorEl] = useState<HTMLElement | null>(null);

  const handleScaleformChange = (scaleform: Scaleform) => {
    selectedMediaPlayer?.setScaleform(scaleform);
  };

  const handleScaleformSave = () => {
    return selectedMediaPlayer?.saveScaleform() ?? Promise.resolve();
  };

  const scaleformSettingsVisible = Boolean(anchorEl);

  const renderModeTooltip = !selectedMediaPlayer?.hasTrack
    ? "MediaPlayer is inactive."
    : selectedMediaPlayer.target.type === TargetType.Screen
      ? "Render mode cannot be changed for scaleform-based targets (screens)."
      : "";

  return (
    selectedMediaPlayer && (
      <Accordion isOpen title="Player Settings">
        <Stack
          p={0.5}
          direction="row"
          gap={2.5}
          width="100%"
          justifyContent="space-evenly"
        >
          <ToggleButton
            value={!selectedMediaPlayer?.isMuted}
            disabled={!selectedMediaPlayer.hasTrack}
            onChange={() =>
              selectedMediaPlayer?.setMuted(!selectedMediaPlayer.muted)
            }
            selected={selectedMediaPlayer?.isMuted}
            tooltipTitle={
              selectedMediaPlayer.hasTrack
                ? `Click to ${selectedMediaPlayer?.isMuted ? "unmute" : "mute"} the player`
                : "MediaPlayer is inactive."
            }
          >
            <VolumeOff />
          </ToggleButton>

          <ToggleButton
            onChange={() =>
              selectedMediaPlayer?.setVideoEnabled(
                !selectedMediaPlayer?.videoEnabled,
              )
            }
            disabled={!selectedMediaPlayer.hasTrack}
            value={!selectedMediaPlayer?.isVideoEnabled}
            selected={selectedMediaPlayer?.isVideoEnabled}
            tooltipTitle={
              selectedMediaPlayer.hasTrack
                ? `Click to ${selectedMediaPlayer?.isVideoEnabled ? "hide" : "show"} the video`
                : "MediaPlayer is inactive."
            }
          >
            <Videocam />
          </ToggleButton>

          <ToggleButton
            tooltipTitle={
              selectedMediaPlayer?.hasTrack
                ? "Click to show Scaleform settings"
                : "MediaPlayer is inactive."
            }
            value={!scaleformSettingsVisible}
            onChange={(event, value) =>
              setAnchorEl(value ? event.currentTarget : null)
            }
            selected={scaleformSettingsVisible}
            disabled={!selectedMediaPlayer.hasTrack}
          >
            <SvgIcon viewBox="0 0 32 32" component={ScaleformIcon} />
          </ToggleButton>
          <Popover
            slotProps={{
              paper: {
                sx: { popoverStyles },
              },
            }}
            open={scaleformSettingsVisible}
            TransitionComponent={Zoom}
            onClose={() => setAnchorEl(null)}
            anchorOrigin={{ vertical: "top", horizontal: "right" }}
            transformOrigin={{
              vertical: "bottom",
              horizontal: "center",
            }}
            anchorEl={anchorEl}
          >
            <ScaleformSettings
              selectedMediaPlayer={selectedMediaPlayer}
              onChange={handleScaleformChange}
              onSave={handleScaleformSave}
              disabled={selectedMediaPlayer.renderMode !== RenderMode.Scaleform}
            />
          </Popover>

          <Select
            items={renderModes}
            label="Render Mode"
            value={selectedMediaPlayer.renderMode}
            onChange={(renderMode) => {
              selectedMediaPlayer?.setRenderMode(
                parseInt(renderMode as string),
              );
            }}
            disabled={!selectedMediaPlayer.canChangeRenderMode}
            tooltipTitle={renderModeTooltip}
          />
        </Stack>
      </Accordion>
    )
  );
};
