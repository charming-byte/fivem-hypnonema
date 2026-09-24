import { AnimatePresence, motion } from "framer-motion";
import {
  type DeepMap,
  type FieldError,
  type FieldValues,
  useController,
  type UseControllerProps,
} from "react-hook-form";
import {
  Box,
  FormHelperText as MuiFormHelperText,
  type FormHelperTextProps,
  TextField as MuiTextField,
} from "@mui/material";
import type { TextFieldProps } from "./UrlInput.tsx";

export type AnimatedTextFieldProps<T extends FieldValues> = TextFieldProps &
  UseControllerProps<T>;
export const AnimatedTextField = motion.create(MuiTextField);

export const AnimatedFormHelperText = motion.create<FormHelperTextProps, "div">(
  MuiFormHelperText,
);

const TextField = <T extends FieldValues>(props: AnimatedTextFieldProps<T>) => {
  const {
    name,
    variant,
    startAdornment,
    required,
    control,
    placeholder,
    label,
    disabled,
  } = props;

  const {
    field: { ref, ...rest },
    formState: { errors },
  } = useController<T>({ name, control });

  return (
    <Box sx={{ width: "100%" }} mb={1}>
      <AnimatedTextField
        required={required}
        disabled={disabled}
        inputRef={ref}
        animate={{
          x: errors[name] ? [30, -30, 15, -15, 8, 0] : 0,
        }}
        transition={{
          duration: 0.55,
          ease: "easeInOut",
          times: [0, 0.3, 0.5, 0.5, 1],
        }}
        label={label}
        style={{ width: "100%", marginBottom: errors[name] ? "9px" : "0" }}
        InputProps={{
          startAdornment: startAdornment,
        }}
        inputProps={{
          style: { color: errors[name] ? "#f44336" : "#fff" },
        }}
        variant={variant || "standard"}
        placeholder={placeholder}
        {...rest}
      />
      <AnimatePresence mode="wait">
        {errors[name] && (
          <AnimatedFormHelperText
            style={{
              marginLeft: 0,
              position: "absolute",
              marginTop: "-3px",
            }}
            error={!!errors[name]}
            initial={{ y: 10, opacity: 0 }}
            animate={{ y: 0, opacity: 1 }}
            exit={{ y: -6, opacity: 0 }}
            transition={{ duration: 0.2 }}
          >
            {(errors[name] as DeepMap<FieldValues, FieldError>).message}
          </AnimatedFormHelperText>
        )}
      </AnimatePresence>
    </Box>
  );
};
export default TextField;
