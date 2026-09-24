import { Stack } from "@mui/material";
import { useMediaPlayers } from "@/hooks/useMediaPlayers.ts";
import { MediaControlPanelHeader } from "./MediaControlPanelHeader.tsx";
import UrlInput from "@/containers/MainView/components/UrlInput/UrlInput.tsx";
import { PlayerSettings } from "../PlayerSettings/PlayerSettings.tsx";

export const MediaControlPanel = () => {
  const { selectedMediaPlayer } = useMediaPlayers();
  return selectedMediaPlayer ? (
    <Stack direction="column" gap={3}>
      <MediaControlPanelHeader
        label={selectedMediaPlayer?.label}
        distance={selectedMediaPlayer?.distance}
      />
      <UrlInput />
      <PlayerSettings />
    </Stack>
  ) : null;
};
