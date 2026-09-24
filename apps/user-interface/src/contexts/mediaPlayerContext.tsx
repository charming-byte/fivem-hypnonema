import { MediaPlayer } from "../models/mediaPlayer.ts";
import { type PropsWithChildren, useEffect, useState } from "react";
import { useNuiEvent } from "../hooks/useNuiEvent.ts";
import { isDevelopmentBrowser } from "../utils/misc.ts";
import { createRandomMediaPlayers } from "../utils/createRandomMediaPlayers.ts";
import {
  NuiEvents,
  type UserInterfaceMediaPlayer,
} from "@hypnonema/generated-types";
import { MediaPlayersContext } from "./mediaPlayerContext.ts";

const convertMediaPlayers = (players: UserInterfaceMediaPlayer[]) => {
  return players.map((player) => new MediaPlayer(player));
};

export const MediaPlayersProvider = ({ children }: PropsWithChildren) => {
  const [mediaPlayers, setMediaPlayers] = useState<MediaPlayer[]>([]);
  const [selectedMediaPlayer, setSelectedMediaPlayer] = useState<
    MediaPlayer | undefined
  >(undefined);

  useNuiEvent(NuiEvents.updateUi, (data: string) => {
    const userInterfaceMediaPlayers = JSON.parse(
      data,
    ) as UserInterfaceMediaPlayer[];
    const updatedMediaPlayers = convertMediaPlayers(userInterfaceMediaPlayers);

    setMediaPlayers(updatedMediaPlayers);

    setSelectedMediaPlayer((current) =>
      current
        ? updatedMediaPlayers.find((p) => p.handle === current.handle)
        : updatedMediaPlayers[0],
    );
  });

  // Nothing refreshes the list while the UI is closed, so clear it on close to
  // avoid showing a stale list on the next open until the first updateUi tick.
  useNuiEvent(NuiEvents.closeUi, () => {
    setMediaPlayers([]);
    setSelectedMediaPlayer(undefined);
  });

  useEffect(() => {
    if (isDevelopmentBrowser()) {
      const remoteMediaPlayers = createRandomMediaPlayers(4);
      const mediaPlayers = convertMediaPlayers(remoteMediaPlayers);
      setMediaPlayers(mediaPlayers);
      setSelectedMediaPlayer(mediaPlayers[0]);
    }
  }, []);

  return (
    <MediaPlayersContext.Provider
      value={{
        mediaPlayers,
        selectedMediaPlayer,
        selectMediaPlayer: setSelectedMediaPlayer,
      }}
    >
      {children}
    </MediaPlayersContext.Provider>
  );
};
