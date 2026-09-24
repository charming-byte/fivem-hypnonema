import { Grid2 as Grid, styled } from "@mui/material";
import { useMediaPlayers } from "@/hooks/useMediaPlayers.ts";
import { MediaPlayerList } from "./components/MediaPlayersList";
import { MediaControlPanel } from "./components/MediaControlPanel/MediaControlPanel.tsx";
import { PlaybackControlsContainer as PlaybackControls } from "./components/PlaybackControls/PlaybackControlsContainer.tsx";
import { AnimatedLayout } from "@components/AnimatedLayout/AnimatedLayout.tsx";

const Container = styled(Grid)({});
const MediaControlPanelContainer = styled(Grid)({});
const MediaSourceListContainer = styled(Grid)({
  borderRight: "1px solid #101010",
  filter: "drop-shadow(1px 0px 0px rgba(100, 100, 100, 0.3))",
  height: "auto",
});
const PlaybackControlsContainer = styled(Grid)({
  borderTop: "1px solid rgba(0,0,0,0.9)",
  boxShadow: "0 -2px 0 -1px  rgba(100, 100, 100, 0.4)",
});

export const MainView = () => {
  const { mediaPlayers, selectedMediaPlayer, selectMediaPlayer } =
    useMediaPlayers();

  return (
    <AnimatedLayout>
      <Container container>
        <MediaSourceListContainer
          style={{ padding: "2rem", height: "405px" }}
          size={{ md: 4 }}
        >
          <MediaPlayerList
            items={mediaPlayers}
            selectedItem={selectedMediaPlayer}
            handleSelect={selectMediaPlayer}
          />
        </MediaSourceListContainer>
        <MediaControlPanelContainer
          size={{ md: 8 }}
          style={{ padding: "2rem" }}
        >
          <MediaControlPanel />
        </MediaControlPanelContainer>
        <PlaybackControlsContainer
          size={{ md: 12 }}
          style={{ padding: "2rem" }}
        >
          <PlaybackControls />
        </PlaybackControlsContainer>
      </Container>
    </AnimatedLayout>
  );
};
