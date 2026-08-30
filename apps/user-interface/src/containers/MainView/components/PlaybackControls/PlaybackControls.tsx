import { Popover, Stack, styled } from "@mui/material";

import {
  Pause,
  PlayArrow,
  QueueMusic,
  Repeat,
  RepeatOn,
  SkipNext,
  Stop,
} from "@mui/icons-material";
import { type MouseEvent } from "react";
import type { MediaPlayer } from "@/models/mediaPlayer.ts";
import IconButton from "@components/IconButton/IconButton.tsx";
import { Queue } from "@/containers/MainView/components/Queue/Queue.tsx";

const Container = styled(Stack)({
  maxHeight: "80px",
  background: "linear-gradient(89.96deg, #2C2B2B 38.71%, #212121 99.96%)",
  boxShadow:
    "inset 0px 3px 0px rgba(0,0,0,0.6), 0px 1px 0px #666666, 1px 0px 0px rgba(0,0,0,0.7),-1px 0 0 rgba(0,0,0,0.7)",
  boxSizing: "border-box",
  borderRadius: "9px",
  display: "flex",
  alignItems: "center",
  position: "relative",
});

const popoverPaperStyle = {
  width: "30rem",
  background: "linear-gradient(89.96deg, #2C2B2B 38.71%, #212121 99.96%)",
  boxSizing: "border-box",
  borderRadius: "12px",
  display: "flex",
};

interface PlaybackControlsProps {
  onStop: () => void;
  onSkipNext: () => void;
  onPause: (paused: boolean) => void;
  onLoop: (looped: boolean) => void;
  queue: {
    anchorEl: HTMLButtonElement | null;
    open: boolean;
    onClose: () => void;
    onClick: (ev: MouseEvent<HTMLButtonElement>) => void;
  };
  disabled?: boolean;
  selectedMediaPlayer?: MediaPlayer;
}

const disabledTooltipText = "MediaPlayer is inactive.";
const adActiveTooltipText = "Playback is disabled during an ad.";

export const PlaybackControls = ({
  onSkipNext,
  disabled,
  onStop,
  onPause,
  onLoop,
  selectedMediaPlayer,
  queue,
}: PlaybackControlsProps) => {
  const { anchorEl, open, onClose, onClick } = queue;

  return (
    <Container direction="row" gap={1}>
      <IconButton
        size="large"
        disabled={disabled}
        tooltip={`${disabled ? disabledTooltipText : "Stop"}`}
        onClick={onStop}
      >
        <Stop />
      </IconButton>

      <IconButton
        disabled={disabled || selectedMediaPlayer?.isQueueEmpty}
        size="large"
        onClick={onSkipNext}
        tooltip={`${disabled ? disabledTooltipText : "Skip Next"}`}
      >
        <SkipNext />
      </IconButton>

      <IconButton
        disabled={disabled || selectedMediaPlayer?.isAdActive}
        size={"large"}
        onClick={() => onPause(!selectedMediaPlayer?.paused)}
        tooltip={
          disabled
            ? disabledTooltipText
            : selectedMediaPlayer?.isAdActive
              ? adActiveTooltipText
              : !selectedMediaPlayer?.isPlaying
                ? "Resume"
                : "Pause"
        }
      >
        {!selectedMediaPlayer?.isPlaying ? <PlayArrow /> : <Pause />}
      </IconButton>

      <IconButton
        size="large"
        disabled={disabled}
        onClick={() => onLoop(!selectedMediaPlayer?.isLooped)}
        tooltip={`${disabled ? disabledTooltipText : "Toggle Repeat"}`}
      >
        {selectedMediaPlayer?.isLooped ? (
          <RepeatOn
            sx={{
              color: !disabled ? "#55076a" : "rgba(95, 8, 119, 0.5)",
            }}
          />
        ) : (
          <Repeat />
        )}
      </IconButton>

      <IconButton
        disabled={disabled || selectedMediaPlayer?.isQueueEmpty}
        tooltip={`${disabled ? disabledTooltipText : selectedMediaPlayer?.isQueueEmpty ? "Queue is empty" : "Queue"}`}
        onClick={onClick}
        size="large"
      >
        <QueueMusic />
      </IconButton>
      <Popover
        slotProps={{
          paper: {
            sx: popoverPaperStyle,
          },
        }}
        open={open}
        anchorEl={anchorEl}
        onClose={onClose}
        anchorOrigin={{ vertical: "top", horizontal: "center" }}
      >
        <Queue queue={selectedMediaPlayer?.queue || []} />{" "}
      </Popover>
    </Container>
  );
};
