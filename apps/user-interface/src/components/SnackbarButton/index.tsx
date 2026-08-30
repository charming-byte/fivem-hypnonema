import { motion } from "framer-motion";
import { Button } from "@mui/material";
import { closeSnackbar, type SnackbarKey } from "notistack";

const AnimatedButton = motion.create(Button);

export const SnackbarButton = ({ id }: { id: SnackbarKey }) => (
  <AnimatedButton
    variant="outlined"
    sx={{
      color: "white",
      borderColor: "white",
      backgroundColor: "transparent",
    }}
    size="small"
    onClick={() => closeSnackbar(id)}
    whileHover={{ scale: 1.03 }}
    whileTap={{ scale: 0.9 }}
  >
    Dismiss
  </AnimatedButton>
);
