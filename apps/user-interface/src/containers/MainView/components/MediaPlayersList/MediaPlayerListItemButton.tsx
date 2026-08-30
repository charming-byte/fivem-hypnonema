import {
  ListItemButton as MuiListItemButton,
  type ListItemButtonProps,
  styled,
} from "@mui/material";
import { motion } from "framer-motion";

export const MediaPlayerListItemButton = styled(
  motion.create(MuiListItemButton),
)<ListItemButtonProps>(() => ({
  "& .MuiListItemSecondaryAction-root": {
    left: "200px",
    top: "40%",
  },
  "&.MuiListItemButton-root": {
    paddingBottom: "0.35rem",
    paddingTop: "0.35rem",
    margin: "0.5rem",
  },
  "&.Mui-selected": {
    background: "rgba(95,8,119,0.4)",
    ":hover": {
      background: "rgba(95,8,119,0.5)",
      width: "92%",
    },
  },
  ":hover": {
    width: "80%",
    background: "rgba(95,8,119,0.25)",
  },
}));
