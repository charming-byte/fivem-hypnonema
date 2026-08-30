import { FC, Fragment } from "react";

import {
  Button,
  Checkbox,
  FormControlLabel,
  FormGroup,
  FormHelperText,
  Grid,
  TextField,
} from "@mui/material";
import Screen from "types/screen";

import { useFormik } from "formik";

interface ScreenFormProps {
  screen?: Screen;
  onSubmit: Function;
}

export const ScreenForm: FC<ScreenFormProps> = (props) => {
  const formik = useFormik({
    initialValues: {
      name: props.screen ? props.screen.name : "",
      maxRenderDistance: props.screen ? props.screen.maxRenderDistance : 400,
      alwaysOn: props.screen ? props.screen.alwaysOn : false,
      is3DRendered: props.screen ? props.screen.is3DRendered : false,
      positionX: props.screen
        ? props.screen.positionalSettings?.positionX || 0
        : 0,
      positionY: props.screen
        ? props.screen.positionalSettings?.positionY || 0
        : 0,
      positionZ: props.screen
        ? props.screen.positionalSettings?.positionZ || 0
        : 0,
      rotationX: props.screen
        ? props.screen.positionalSettings?.rotationX || 0
        : 0,
      rotationY: props.screen
        ? props.screen.positionalSettings?.rotationY || 0
        : 0,
      rotationZ: props.screen
        ? props.screen.positionalSettings?.rotationZ || 0
        : 0,
      scaleX: props.screen ? props.screen.positionalSettings?.scaleX || 0 : 0,
      scaleY: props.screen ? props.screen.positionalSettings?.scaleY || 0 : 0,
      scaleZ: props.screen ? props.screen.positionalSettings?.scaleZ || 0 : 0,
      soundMinDistance: props.screen
        ? props.screen.browserSettings?.soundMinDistance!
        : 30,
      soundMaxDistance: props.screen
        ? props.screen.browserSettings?.soundMaxDistance!
        : 200,
      soundAttenuation: props.screen
        ? props.screen.browserSettings?.soundAttenuation!
        : 5,
      modelName: props.screen ? props.screen.targetSettings?.modelName : "",
      renderTargetName: props.screen
        ? props.screen.targetSettings?.renderTargetName
        : "",
    },
    validate: (values) => {
      const errors: any = {};

      if (!values.name) {
        errors.name = "Screen Name is required";
      }

      if (isNaN(parseInt(values.soundMinDistance.toString()))) {
        errors.soundMinDistance = "Sound Min. Distance is required";
      }
      if (isNaN(parseInt(values.soundMaxDistance.toString()))) {
        errors.soundMaxDistance = "Sound Max. Distance is required";
      }

      if (isNaN(parseInt(values.soundAttenuation.toString()))) {
        errors.soundAttenuation = "Sound Attenuation is required";
      }

      if (isNaN(parseInt(values.maxRenderDistance.toString()))) {
        errors.maxRenderDistance = "Max. Render Distance is required";
      }

      if (!values.is3DRendered) {
        if (!values.renderTargetName) {
          errors.renderTargetName = "RenderTarget Name is required";
        }
        if (!values.modelName) {
          errors.modelName = "Model Name is required";
        }
      } else {
        if (isNaN(parseFloat(values.positionX.toString()))) {
          errors.positionX = "PositionX is required";
        }
        if (isNaN(parseFloat(values.positionY.toString()))) {
          errors.positionY = "PositionY is required";
        }
        if (isNaN(parseFloat(values.positionZ.toString()))) {
          errors.positionZ = "PositionZ is required";
        }
        if (isNaN(parseFloat(values.rotationX.toString()))) {
          errors.rotationX = "RotationX is required";
        }
        if (isNaN(parseFloat(values.rotationY.toString()))) {
          errors.rotationY = "RotationY is required";
        }
        if (isNaN(parseFloat(values.rotationZ.toString()))) {
          errors.rotationZ = "RotationZ is required";
        }
        if (isNaN(parseFloat(values.scaleX.toString()))) {
          errors.scaleX = "ScaleX is required";
        }
        if (isNaN(parseFloat(values.scaleY.toString()))) {
          errors.scaleY = "ScaleY is required";
        }
        if (isNaN(parseFloat(values.scaleZ.toString()))) {
          errors.scaleZ = "ScaleZ is required";
        }
      }
      return errors;
    },
    onSubmit: (values) => {
      const screen = {
        id: props.screen?.id || undefined,
        name: values.name,
        targetSettings: {
          modelName: values.modelName,
          renderTargetName: values.renderTargetName,
        },
        browserSettings: {
          is3DAudioEnabled: true,
          soundAttenuation: values.soundAttenuation,
          soundMinDistance: values.soundMinDistance,
          soundMaxDistance: values.soundMaxDistance,
          globalVolume: 100,
        },
        positionalSettings: {
          positionX: values.positionX,
          positionY: values.positionY,
          positionZ: values.positionZ,
          rotationX: values.rotationX,
          rotationY: values.rotationY,
          rotationZ: values.rotationZ,
          scaleX: values.scaleX,
          scaleY: values.scaleY,
          scaleZ: values.scaleZ,
        },
        alwaysOn: values.alwaysOn,
        maxRenderDistance: values.maxRenderDistance,
        is3DRendered: values.is3DRendered,
      }
      props.onSubmit(screen);
    },
  });

  return (
    <div>
      <form onSubmit={formik.handleSubmit}>
        <Grid
          container
          spacing={{ xs: 2, md: 2 }}
          columns={{ xs: 2, sm: 8, md: 16 }}
        >
          <Grid item xs={6} md={11}>
            <TextField
              value={formik.values.name}
              fullWidth
              error={formik.touched.name && Boolean(formik.errors.name)}
              helperText={formik.touched.name && formik.errors.name}
              onChange={formik.handleChange}
              label="Screen Name"
              name="name"
              sx={{ maxWidth: 518, width: 518 }}
            />
          </Grid>
          <Grid item>
            <FormGroup>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={formik.values.alwaysOn}
                    onChange={formik.handleChange}
                    name="alwaysOn"
                  />
                }
                label="Always On"
              />
              <FormHelperText>
                Whether screen stays open after playback
              </FormHelperText>
            </FormGroup>
          </Grid>
          <Grid item md={8}>
            <FormGroup>
              <FormControlLabel
                control={
                  <Checkbox
                    checked={formik.values.is3DRendered}
                    onChange={formik.handleChange}
                    name="is3DRendered"
                  />
                }
                label="Is 3D Rendered"
              />
            </FormGroup>
          </Grid>
          <Grid item md={12}>
            <TextField
              value={formik.values.maxRenderDistance}
              fullWidth
              type="number"
              InputProps={{ inputProps: { min: 1 } }}
              error={
                formik.touched.maxRenderDistance &&
                Boolean(formik.errors.maxRenderDistance)
              }
              helperText={
                formik.touched.maxRenderDistance &&
                formik.errors.maxRenderDistance
              }
              onChange={formik.handleChange}
              label="Max. Render Distance"
              name="maxRenderDistance"
              sx={{ maxWidth: 518, width: 518 }}
            />
          </Grid>
          {!formik.values.is3DRendered && (
            <Fragment>
              <Grid item>
                <TextField
                  value={formik.values.renderTargetName}
                  error={
                    formik.touched.renderTargetName &&
                    Boolean(formik.errors.renderTargetName)
                  }
                  helperText={
                    formik.touched.renderTargetName &&
                    formik.errors.renderTargetName
                  }
                  onChange={formik.handleChange}
                  label="RenderTarget Name"
                  name="renderTargetName"
                  variant="standard"
                  sx={{ minWidth: "200px" }}
                />
              </Grid>
              <Grid item md={8}>
                <TextField
                  value={formik.values.modelName}
                  error={
                    formik.touched.modelName && Boolean(formik.errors.modelName)
                  }
                  helperText={
                    formik.touched.modelName && formik.errors.modelName
                  }
                  onChange={formik.handleChange}
                  label="Model Name"
                  name="modelName"
                  variant="standard"
                  sx={{ minWidth: "200px" }}
                />
              </Grid>
            </Fragment>
          )}
          {formik.values.is3DRendered && (
            <Fragment>
              <Grid item>
                <TextField
                  label="Position X"
                  name="positionX"
                  value={formik.values.positionX}
                  error={
                    formik.touched.positionX && Boolean(formik.errors.positionX)
                  }
                  helperText={
                    formik.touched.positionX && formik.errors.positionX
                  }
                  onChange={formik.handleChange}
                  type="number"
                  InputLabelProps={{
                    shrink: true,
                  }}
                />
              </Grid>
              <Grid item>
                <TextField
                  label="Position Y"
                  name="positionY"
                  value={formik.values.positionY}
                  onChange={formik.handleChange}
                  error={
                    formik.touched.positionY && Boolean(formik.errors.positionY)
                  }
                  helperText={
                    formik.touched.positionY && formik.errors.positionY
                  }
                  type="number"
                  InputLabelProps={{
                    shrink: true,
                  }}
                />
              </Grid>
              <Grid item>
                <TextField
                  label="Position Z"
                  name="positionZ"
                  value={formik.values.positionZ}
                  onChange={formik.handleChange}
                  error={
                    formik.touched.positionZ && Boolean(formik.errors.positionZ)
                  }
                  helperText={
                    formik.touched.positionZ && formik.errors.positionZ
                  }
                  type="number"
                  InputLabelProps={{
                    shrink: true,
                  }}
                />
              </Grid>
              <Grid item>
                <TextField
                  label="Rotation X"
                  name="rotationX"
                  value={formik.values.rotationX}
                  onChange={formik.handleChange}
                  error={
                    formik.touched.rotationX && Boolean(formik.errors.rotationX)
                  }
                  helperText={
                    formik.touched.rotationX && formik.errors.rotationX
                  }
                  type="number"
                  InputLabelProps={{
                    shrink: true,
                  }}
                />
              </Grid>
              <Grid item>
                <TextField
                  label="Rotation Y"
                  name="rotationY"
                  value={formik.values.rotationY}
                  onChange={formik.handleChange}
                  error={
                    formik.touched.rotationY && Boolean(formik.errors.rotationY)
                  }
                  helperText={
                    formik.touched.rotationY && formik.errors.rotationY
                  }
                  type="number"
                  InputLabelProps={{
                    shrink: true,
                  }}
                />
              </Grid>
              <Grid item>
                <TextField
                  label="Rotation Z"
                  name="rotationZ"
                  value={formik.values.rotationZ}
                  error={
                    formik.touched.rotationZ && Boolean(formik.errors.rotationZ)
                  }
                  helperText={
                    formik.touched.rotationZ && formik.errors.rotationZ
                  }
                  onChange={formik.handleChange}
                  type="number"
                  InputLabelProps={{
                    shrink: true,
                  }}
                />
              </Grid>
              <Grid item>
                <TextField
                  label="Scale X"
                  name="scaleX"
                  value={formik.values.scaleX}
                  onChange={formik.handleChange}
                  error={formik.touched.scaleX && Boolean(formik.errors.scaleX)}
                  helperText={formik.touched.scaleX && formik.errors.scaleX}
                  type="number"
                  InputLabelProps={{
                    shrink: true,
                  }}
                />
              </Grid>
              <Grid item>
                <TextField
                  label="Scale Y"
                  name="scaleY"
                  value={formik.values.scaleY}
                  onChange={formik.handleChange}
                  error={formik.touched.scaleY && Boolean(formik.errors.scaleY)}
                  helperText={formik.touched.scaleY && formik.errors.scaleY}
                  type="number"
                  InputLabelProps={{
                    shrink: true,
                  }}
                />
              </Grid>
              <Grid item>
                <TextField
                  label="Scale Z"
                  name="scaleZ"
                  value={formik.values.scaleZ}
                  onChange={formik.handleChange}
                  error={formik.touched.scaleZ && Boolean(formik.errors.scaleZ)}
                  helperText={formik.touched.scaleZ && formik.errors.scaleZ}
                  type="number"
                  InputLabelProps={{
                    shrink: true,
                  }}
                />
              </Grid>
            </Fragment>
          )}

          <Grid item>
            <TextField
              label="Sound Min. Distance"
              name="soundMinDistance"
              error={
                formik.touched.soundMinDistance &&
                Boolean(formik.errors.soundMinDistance)
              }
              helperText={
                formik.touched.soundMinDistance &&
                formik.errors.soundMinDistance
              }
              value={formik.values.soundMinDistance}
              onChange={formik.handleChange}
              type="number"
              InputLabelProps={{
                shrink: true,
              }}
            />
          </Grid>
          <Grid item>
            <TextField
              label="Sound Max. Distance"
              name="soundMaxDistance"
              error={
                formik.touched.soundMaxDistance &&
                Boolean(formik.errors.soundMaxDistance)
              }
              helperText={
                formik.touched.soundMaxDistance &&
                formik.errors.soundMaxDistance
              }
              value={formik.values.soundMaxDistance}
              onChange={formik.handleChange}
              type="number"
              InputLabelProps={{
                shrink: true,
              }}
            />
          </Grid>

          <Grid item>
            <TextField
              label="Sound Attenuation"
              name="soundAttenuation"
              value={formik.values.soundAttenuation}
              onChange={formik.handleChange}
              error={
                formik.touched.soundAttenuation &&
                Boolean(formik.errors.soundAttenuation)
              }
              helperText={
                formik.touched.soundAttenuation &&
                formik.errors.soundAttenuation
              }
              type="number"
              InputLabelProps={{
                shrink: true,
              }}
            />
          </Grid>
          <Grid item md={8} sx={{ marginTop: "auto" }}>
            <Button
              variant="contained"
              type="submit"
              sx={{
                marginRight: "20px",
                marginBottom: "5px",
              }}
            >
              Submit
            </Button>
          </Grid>
        </Grid>
      </form>
    </div>
  );
};
