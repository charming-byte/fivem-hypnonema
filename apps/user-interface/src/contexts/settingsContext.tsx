import { AudioMode, NuiEvents } from "@hypnonema/generated-types";
import { type PropsWithChildren, useState } from "react";
import { useNuiEvent } from "@/hooks/useNuiEvent.ts";
import {
  defaultSettingsContextValue,
  SettingsContext,
} from "./settingsContext.ts";

export const SettingsProvider = ({ children }: PropsWithChildren) => {
  const [audioMode, setAudioMode] = useState<AudioMode>(
    defaultSettingsContextValue.audioMode,
  );

  useNuiEvent(NuiEvents.setAudioMode, (audioMode: AudioMode) => {
    setAudioMode(audioMode);
  });

  return (
    <SettingsContext.Provider value={{ audioMode }}>
      {children}
    </SettingsContext.Provider>
  );
};
