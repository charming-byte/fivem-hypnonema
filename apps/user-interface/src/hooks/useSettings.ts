import { useContext } from "react";
import { SettingsContext } from "@/contexts/settingsContext.ts";

export const useSettings = () => {
  const context = useContext(SettingsContext);

  if (context === undefined)
    throw new Error("useMediaPlayers must be used within a SettingsProvider");

  return context;
};
