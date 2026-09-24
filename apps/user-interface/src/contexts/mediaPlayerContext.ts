import { createContext } from "react";
import { MediaPlayer } from "../models/mediaPlayer.ts";

export type MediaPlayersContextType = {
  mediaPlayers: MediaPlayer[];
  selectedMediaPlayer: MediaPlayer | undefined;
  selectMediaPlayer: (player: MediaPlayer) => void;
};

export const defaultMediaPlayersContextValue: MediaPlayersContextType = {
  mediaPlayers: [],
  selectedMediaPlayer: undefined,
  selectMediaPlayer: () => {},
};

export const MediaPlayersContext = createContext<MediaPlayersContextType>(
  defaultMediaPlayersContextValue,
);
