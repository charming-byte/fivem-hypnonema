import { Stack, styled, Typography as MuiTypography } from "@mui/material";
import { NumberInput } from "@/containers/MainView/components/NumberInput/NumberInput.tsx";
import { type ReactNode } from "react";
import Grid from "@mui/material/Grid2";
import type {
  ScaleformRenderConfiguration as Scaleform,
  Vector3,
} from "@hypnonema/generated-types";
import {
  defaultScaleformSettings,
  defaultScaleformSteps,
  type ScaleformSteps,
} from "@/containers/MainView/components/ScaleformSettings/defaultScaleformSettings.ts";

// The inputs below only edit the Vector3-valued members of ScaleformSettings.
type VectorProperty = "position" | "rotation" | "scale";

interface ScaleformSettingsInput {
  value?: Scaleform;
  onChange: (scaleform: Scaleform) => void;
  /** Called when a value is submitted with Enter, to bypass the pending debounce. */
  onCommit?: () => void;
  disabled?: boolean;
  steps?: ScaleformSteps;
  onStepsChange?: (property: VectorProperty, value: number) => void;
}

const Group = ({ children }: { children: ReactNode }) => (
  <Stack direction="column" alignItems="center" gap={1}>
    {children}
  </Stack>
);

const Label = ({ children }: { children: ReactNode }) => (
  <Typography variant="caption" fontSize={13}>
    {children}
  </Typography>
);

const Typography = styled(MuiTypography)(() => ({
  "&.MuiTypography-root": {
    userSelect: "none",
  },
}));

export const ScaleformSettingsInput = ({
  value: scaleform = defaultScaleformSettings,
  onChange,
  onCommit,
  disabled = false,
  steps = defaultScaleformSteps,
  onStepsChange,
}: ScaleformSettingsInput) => {
  const handleChange = (
    val: number | null,
    property: VectorProperty,
    dimension: keyof Vector3,
  ) => {
    onChange({
      ...scaleform,
      transform: {
        ...scaleform.transform,
        [property]: {
          ...scaleform.transform[property],
          [dimension]: val || 0,
        },
      },
    });
  };

  const handleStepChange = (val: number | null, property: VectorProperty) => {
    onStepsChange?.(
      property,
      val && val > 0 ? val : defaultScaleformSteps[property],
    );
  };

  return (
    <Grid alignItems="center" p={1} m={1} width="100%" container spacing={2}>
      <Grid size={4}>
        <Group>
          <Label>Position</Label>
          <NumberInput
            label="X"
            value={scaleform.transform.position.x}
            step={steps.position}
            disabled={disabled}
            onCommit={onCommit}
            onValueChange={(value) => handleChange(value, "position", "x")}
          />
          <NumberInput
            label="Y"
            value={scaleform.transform.position.y}
            step={steps.position}
            disabled={disabled}
            onCommit={onCommit}
            onValueChange={(value) => handleChange(value, "position", "y")}
          />
          <NumberInput
            label="Z"
            value={scaleform.transform.position.z}
            step={steps.position}
            disabled={disabled}
            onCommit={onCommit}
            onValueChange={(value) => handleChange(value, "position", "z")}
          />
          <NumberInput
            label="Step"
            value={steps.position}
            step={0.1}
            min={0.01}
            disabled={disabled}
            onValueChange={(value) => handleStepChange(value, "position")}
          />
        </Group>
      </Grid>
      <Grid size={4}>
        <Group>
          <Label>Rotation</Label>
          <NumberInput
            label="X"
            value={scaleform.transform.rotation.x}
            step={steps.rotation}
            disabled={disabled}
            onCommit={onCommit}
            onValueChange={(value) => handleChange(value, "rotation", "x")}
          />
          <NumberInput
            label="Y"
            value={scaleform.transform.rotation.y}
            step={steps.rotation}
            disabled={disabled}
            onCommit={onCommit}
            onValueChange={(value) => handleChange(value, "rotation", "y")}
          />
          <NumberInput
            label="Z"
            value={scaleform.transform.rotation.z}
            step={steps.rotation}
            disabled={disabled}
            onCommit={onCommit}
            onValueChange={(value) => handleChange(value, "rotation", "z")}
          />
          <NumberInput
            label="Step"
            value={steps.rotation}
            step={0.1}
            min={0.01}
            disabled={disabled}
            onValueChange={(value) => handleStepChange(value, "rotation")}
          />
        </Group>
      </Grid>
      <Grid size={4}>
        <Group>
          <Label>Scale</Label>
          <NumberInput
            label="X"
            value={scaleform.transform.scale.x}
            step={steps.scale}
            disabled={disabled}
            onCommit={onCommit}
            onValueChange={(value) => handleChange(value, "scale", "x")}
          />
          <NumberInput
            label="Y"
            value={scaleform.transform.scale.y}
            step={steps.scale}
            disabled={disabled}
            onCommit={onCommit}
            onValueChange={(value) => handleChange(value, "scale", "y")}
          />
          <NumberInput
            label="Z"
            value={scaleform.transform.scale.z}
            step={steps.scale}
            disabled={disabled}
            onCommit={onCommit}
            onValueChange={(value) => handleChange(value, "scale", "z")}
          />
          <NumberInput
            label="Step"
            value={steps.scale}
            step={0.1}
            min={0.01}
            disabled={disabled}
            onValueChange={(value) => handleStepChange(value, "scale")}
          />
        </Group>
      </Grid>
    </Grid>
  );
};
