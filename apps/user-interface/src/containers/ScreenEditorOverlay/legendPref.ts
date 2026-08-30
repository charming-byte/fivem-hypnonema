// The native shell owns F1 (the NUI page never has focus) and just forwards the toggle. The visible
// state and its cross-restart persistence live here - the ticket's "beim ersten Öffnen sichtbar,
// danach client-lokal gemerkt". Default is shown; only an explicit hide is remembered.
const LEGEND_PREF_KEY = "hypnonema.screenEditor.legend";

export const readLegendPref = (): boolean => {
  try {
    return localStorage.getItem(LEGEND_PREF_KEY) !== "hidden";
  } catch {
    return true;
  }
};

export const persistLegendPref = (visible: boolean): void => {
  try {
    localStorage.setItem(LEGEND_PREF_KEY, visible ? "shown" : "hidden");
  } catch {
    /* private mode / storage disabled - the toggle still works for this session */
  }
};
