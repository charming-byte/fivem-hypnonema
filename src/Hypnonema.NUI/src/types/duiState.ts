import Screen from "./screen";

export interface DuiState {
  screenName: string;
  isPaused: boolean;
  startedAt: string;
  duration: number;
  currentSource: string;
  repeat: boolean;
  screen: Screen;
}
