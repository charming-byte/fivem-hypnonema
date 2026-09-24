import { Stack } from "@mui/material";
import { KnobBase } from "@/containers/MainView/components/Knobs/KnobBase.tsx";
import { mapFrom01Linear, mapTo01Linear } from "@dsp-ts/math";
import type { SpatializedAudio } from "@/types/spatializedAudio.ts";
import { Accordion } from "@components/Accordion/index.ts";

type AudioSettingsProps = {
  disabled?: boolean;
  onChange: ({
    maxVolume,
    soundMaxDistance,
    soundMinDistance,
    rollOffFactor,
  }: SpatializedAudio) => void;
  maxVolume?: number;
  rollOffFactor?: number;
  soundMinDistance?: number;
  soundMaxDistance?: number;
};

export const AudioSettings = ({
  disabled = false,
  onChange,
  maxVolume = 100,
  rollOffFactor = 0.5,
  soundMaxDistance = 100,
  soundMinDistance = 10,
}: AudioSettingsProps) => {
  return (
    <Accordion title="Audio Settings">
      <Stack
        direction="row"
        width="100%"
        ml={2}
        justifyContent="space-between"
        sx={{ pointerEvents: disabled ? "none" : "auto" }}
      >
        <KnobBase
          label="Max Volume"
          tooltip="The maximum allowed volume level."
          disabled={disabled}
          valueDefault={maxVolume}
          stepFn={(value) => value + 1}
          stepLargerFn={(value) => value + 10}
          orientation="vertical"
          valueMin={1}
          valueMax={100}
          mapTo01={mapTo01Linear}
          mapFrom01={mapFrom01Linear}
          handleChange={(valueRaw) =>
            onChange({
              maxVolume: Math.round(valueRaw),
              soundMaxDistance,
              soundMinDistance,
              rollOffFactor,
            })
          }
          valueRawDisplayFn={(valueRaw) => Math.round(valueRaw).toString()}
          valueRawRoundFn={Math.round}
        />

        <KnobBase
          label="Max Distance"
          tooltip=" If the distance is greater than or equal to the maximum
                    distance, the sound is not heard (volume is zero)."
          valueDefault={soundMaxDistance}
          stepFn={(value) => value + 1}
          stepLargerFn={(value) => value + 10}
          orientation="vertical"
          valueMin={soundMinDistance}
          valueMax={200}
          mapTo01={mapTo01Linear}
          mapFrom01={mapFrom01Linear}
          handleChange={(valueRaw) =>
            onChange({
              soundMaxDistance: Math.round(valueRaw),
              soundMinDistance,
              rollOffFactor,
              maxVolume,
            })
          }
          valueRawDisplayFn={(valueRaw) => Math.round(valueRaw).toString()}
          valueRawRoundFn={Math.round}
        />

        <KnobBase
          label="Min Distance"
          tooltip=" If the distance is less than or equal to the minimum
                    distance, the sound is at maximum volume."
          valueDefault={soundMinDistance}
          stepFn={(value) => value + 1}
          stepLargerFn={(value) => value + 10}
          orientation="vertical"
          valueMin={1}
          valueMax={soundMaxDistance}
          mapTo01={mapTo01Linear}
          mapFrom01={mapFrom01Linear}
          handleChange={(valueRaw) =>
            onChange({
              soundMinDistance: Math.round(valueRaw),
              soundMaxDistance,
              rollOffFactor,
              maxVolume,
            })
          }
          valueRawDisplayFn={(valueRaw) => Math.round(valueRaw).toString()}
          valueRawRoundFn={Math.round}
        />

        <KnobBase
          label="Roll Off Factor"
          tooltip=" For distances between the minimum and maximum distances, the
                    volume decreases based on the roll-off factor."
          valueDefault={rollOffFactor}
          stepFn={(value) => value + 0.1}
          stepLargerFn={(value) => value + 0.2}
          orientation="vertical"
          valueMin={0.1}
          valueMax={1}
          mapTo01={mapTo01Linear}
          handleChange={(valueRaw) =>
            onChange({
              rollOffFactor: Math.round(valueRaw * 10) / 10,
              soundMaxDistance,
              soundMinDistance,
              maxVolume,
            })
          }
          mapFrom01={mapFrom01Linear}
          valueRawDisplayFn={(valueRaw) => valueRaw.toFixed(1).toString()}
          valueRawRoundFn={Math.round}
        />
      </Stack>
    </Accordion>
  );
};
