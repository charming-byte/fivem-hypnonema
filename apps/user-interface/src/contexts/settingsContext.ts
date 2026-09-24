import { AudioMode } from "@hypnonema/generated-types";
import { createContext } from "react";

export type SettingsContextType = {
  audioMode: AudioMode;
};

export const defaultSettingsContextValue: SettingsContextType = {
  audioMode: AudioMode.Default,
};

export const SettingsContext = createContext<SettingsContextType>(
  defaultSettingsContextValue,
);
