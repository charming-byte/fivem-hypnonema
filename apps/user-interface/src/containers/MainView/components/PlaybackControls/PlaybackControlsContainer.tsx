import { type MouseEvent, useState } from "react";
import { Grid2 as Grid, Stack, styled } from "@mui/material";
import { MediaThumbnail } from "@/containers/MainView/components/MediaThumbnail/MediaThumbnail.tsx";
import { PlaybackControls } from "@/containers/MainView/components/PlaybackControls/PlaybackControls.tsx";
import TimeSlider from "@/containers/MainView/components/TimeSlider/TimeSlider.tsx";
import VolumeSlider from "@/containers/MainView/components/VolumeSlider/VolumeSlider.tsx";
import { useMediaPlayers } from "@/hooks/useMediaPlayers.ts";

const Container = styled(Grid)({
  boxShadow: "none !important",
  display: "flex",
  alignItems: "flex-start",
});

export const PlaybackControlsContainer = () => {
  const { selectedMediaPlayer } = useMediaPlayers();
  const [anchorEl, setAnchorEl] = useState<HTMLButtonElement | null>(null);
  const queueOpen = Boolean(anchorEl);

  const handleStop = () => selectedMediaPlayer?.stop();
  const handleSkipNext = () => selectedMediaPlayer?.skipNext();
  const handleSeek = (position: number) => selectedMediaPlayer?.seek(position);

  const handlePause = (paused: boolean) =>
    selectedMediaPlayer?.setPaused(paused);

  const handleLoop = (looped: boolean) =>
    selectedMediaPlayer?.setLooped(looped);

  const handleVolumeChange = (volume: number) =>
    selectedMediaPlayer?.setVolume(Math.ceil(volume));

  const handleClose = () => setAnchorEl(null);
  const handleClick = (event: MouseEvent<HTMLButtonElement>) =>
    setAnchorEl(event.currentTarget);

  return (
    <Container
      container
      sx={{ justifyContent: "center", alignItems: "center", height: "11rem" }}
    >
      <Grid size={{ xs: 3 }} style={{ height: "100%" }}>
        <MediaThumbnail track={selectedMediaPlayer?.track} />
      </Grid>
      <Grid
        size={{ xs: 6 }}
        display="flex"
        alignItems="center"
        justifyContent="center"
        height="100%"
      >
        <Stack height="100%" justifyContent="space-between">
          <PlaybackControls
            disabled={!selectedMediaPlayer?.hasTrack}
            onStop={handleStop}
            onPause={handlePause}
            onSkipNext={handleSkipNext}
            onLoop={handleLoop}
            queue={{
              onClose: handleClose,
              onClick: handleClick,
              open: queueOpen,
              anchorEl,
            }}
            selectedMediaPlayer={selectedMediaPlayer}
          />
          <TimeSlider
            disabled={
              !selectedMediaPlayer?.hasTrack || selectedMediaPlayer?.isStream
            }
            max={selectedMediaPlayer?.track?.duration}
            value={selectedMediaPlayer?.track?.position}
            paused={selectedMediaPlayer?.paused}
            onChange={handleSeek}
          />
        </Stack>
      </Grid>
      <Grid
        size={{ xs: 3 }}
        display="flex"
        alignItems="flex-start"
        justifyContent="center"
        marginLeft="auto"
        height="100%"
      >
        <VolumeSlider
          onChange={handleVolumeChange}
          min={0}
          max={100}
          step={5}
          value={(selectedMediaPlayer?.volume || 0) * 100}
          disabled={!selectedMediaPlayer?.hasTrack}
        />
      </Grid>
    </Container>
  );
};
