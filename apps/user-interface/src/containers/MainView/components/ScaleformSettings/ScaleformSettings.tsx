import {
  Alert,
  Box,
  Button,
  Stack,
  styled,
  Tooltip,
  Typography as MuiTypography,
} from "@mui/material";
import debounce from "lodash.debounce";
import { useEffect, useMemo, useRef, useState } from "react";
import { MediaPlayer } from "@/models/mediaPlayer.ts";
import type { ScaleformRenderConfiguration as Scaleform } from "@hypnonema/generated-types";
import { ScaleformSettingsInput } from "@/containers/MainView/components/ScaleformSettings/ScaleformSettingsInput.tsx";
import {
  defaultScaleformSettings,
  defaultScaleformSteps,
  type ScaleformSteps,
} from "@/containers/MainView/components/ScaleformSettings/defaultScaleformSettings.ts";
import { useToast } from "@/hooks/useToast.ts";

const DEBOUNCE_DELAY = 750;
const STEPS_STORAGE_KEY = "hypnonema.scaleformSettings.steps";

const loadScaleformSteps = (): ScaleformSteps => {
  try {
    const stored = localStorage.getItem(STEPS_STORAGE_KEY);
    if (!stored) {
      return defaultScaleformSteps;
    }
    return { ...defaultScaleformSteps, ...JSON.parse(stored) };
  } catch {
    return defaultScaleformSteps;
  }
};

interface ScaleformSettingsProps {
  onChange?: (scaleform: Scaleform) => void;
  onSave?: () => Promise<void>;
  selectedMediaPlayer?: MediaPlayer;
  disabled?: boolean;
}

const Typography = styled(MuiTypography)(() => ({
  "&.MuiTypography-root": {
    userSelect: "none",
    fontSize: "14px",
  },
}));

const Background = styled(Box)(() => ({
  flexBasis: "100%",
  minWidth: "45rem",
  background: "linear-gradient(89.96deg, #2C2B2B 38.71%, #212121 99.96%)",
  boxShadow:
    "inset 0px 3px 0px rgba(0,0,0,0.6), 1px 0px 0px rgba(0,0,0,0.7),-1px 0 0 rgba(0,0,0,0.7)",
  boxSizing: "border-box",
  borderRadius: "5px",
  display: "flex",
}));

export const ScaleformSettings = ({
  onChange = () => {},
  onSave,
  selectedMediaPlayer,
  disabled = false,
}: ScaleformSettingsProps) => {
  const { showToast } = useToast();
  const handle = selectedMediaPlayer?.handle;

  // Working copy the inputs render from. selectedMediaPlayer is a fresh instance on
  // every updateUi push, so it is only re-seeded when a different player is selected —
  // otherwise incoming pushes would overwrite values while they are being edited.
  const [scaleform, setScaleform] = useState<Scaleform>(
    selectedMediaPlayer?.scaleform ?? defaultScaleformSettings,
  );
  const [seededHandle, setSeededHandle] = useState(handle);

  if (handle !== seededHandle) {
    setSeededHandle(handle);
    setScaleform(selectedMediaPlayer?.scaleform ?? defaultScaleformSettings);
  }

  // Per-group increment/decrement step sizes. Purely a local editing
  // preference (not synced to media players), so it's persisted to
  // localStorage instead of going through onChange/RPC.
  const [steps, setSteps] = useState<ScaleformSteps>(loadScaleformSteps);

  useEffect(() => {
    localStorage.setItem(STEPS_STORAGE_KEY, JSON.stringify(steps));
  }, [steps]);

  const handleStepsChange = (property: keyof ScaleformSteps, value: number) => {
    setSteps((prev) => ({ ...prev, [property]: value }));
  };

  // Held in a ref so the debounced emitter keeps a stable identity across those same
  // pushes; memoizing on onChange itself would rebuild it before the delay elapses.
  const onChangeRef = useRef(onChange);
  useEffect(() => {
    onChangeRef.current = onChange;
  }, [onChange]);

  const debouncedChangeHandler = useMemo(
    () =>
      debounce(
        (value: Scaleform) => onChangeRef.current(value),
        DEBOUNCE_DELAY,
      ),
    [],
  );

  useEffect(
    () => () => debouncedChangeHandler.cancel(),
    [debouncedChangeHandler],
  );

  const handleChange = (value: Scaleform) => {
    setScaleform(value);
    debouncedChangeHandler(value);
  };

  // Submitting a value with Enter is an explicit "apply now", so don't sit on the
  // debounce delay.
  const handleCommit = () => debouncedChangeHandler.flush();

  const handleSave = async () => {
    try {
      await onSave?.();
      showToast("Scaleform settings saved.");
    } catch {
      showToast("Failed to save scaleform settings.");
    }
  };

  const canSave = !disabled && selectedMediaPlayer?.canSaveScaleform;

  return (
    <Background m={2}>
      <Stack width="100%" gap={1} p={1} alignItems="center" direction="column">
        <Typography mt={2} variant="caption">
          Scaleform Settings
        </Typography>
        <ScaleformSettingsInput
          value={scaleform}
          onChange={handleChange}
          onCommit={handleCommit}
          disabled={disabled}
          steps={steps}
          onStepsChange={handleStepsChange}
        />
        <Stack
          justifyContent="flex-end"
          alignItems="center"
          direction="row"
          gap={2}
          width="100%"
        >
          {disabled && (
            <Alert severity="info" variant="outlined">
              Scaleform Settings can only be changed in &quot;Floating 3D
              Display&quot; render mode!
            </Alert>
          )}
          <Tooltip
            title={
              !disabled && !selectedMediaPlayer?.canSaveScaleform
                ? "Saving to disk is only available for screen-based media players."
                : ""
            }
          >
            <span>
              <Button
                variant="contained"
                disabled={!canSave}
                onClick={handleSave}
              >
                Save
              </Button>
            </span>
          </Tooltip>
        </Stack>
      </Stack>
    </Background>
  );
};
