import { useContext } from "react";
import { MediaPlayersContext } from "../contexts/mediaPlayerContext.ts";

export const useMediaPlayers = () => {
  const context = useContext(MediaPlayersContext);

  if (context === undefined)
    throw new Error(
      "useMediaPlayers must be used within a MediaPlayersProvider",
    );

  return context;
};
