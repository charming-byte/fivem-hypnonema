import { IconButton as MuiIconButton, styled } from "@mui/material";
import type { IconButtonProps as MuiIconButtonProps } from "@mui/material";
import { motion } from "framer-motion";
import type { MotionProps } from "framer-motion";
import type { StyledComponent } from "@emotion/styled";

type IconButtonProps = MuiIconButtonProps & MotionProps;

const StyledIconButton: StyledComponent<IconButtonProps> = styled(
  motion.create(MuiIconButton),
)<IconButtonProps>(() => ({
  "&.MuiIconButton-root": {
    "&:hover": {
      backgroundColor: "transparent",
    },
  },
}));
export default StyledIconButton;
