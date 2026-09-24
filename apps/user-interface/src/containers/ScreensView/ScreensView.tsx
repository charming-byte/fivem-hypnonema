import { useState } from "react";
import { useNavigate } from "react-router";
import {
  Alert,
  Box,
  Button,
  Chip,
  List,
  ListItemButton,
  ListItemText,
  styled,
  Typography,
} from "@mui/material";
import SimpleBar from "simplebar-react";
import "simplebar-react/dist/simplebar.min.css";
import type { Variants } from "framer-motion";
import { AnimatedLayout } from "@components/AnimatedLayout/AnimatedLayout.tsx";
import { useScreens } from "@/hooks/useScreens.ts";
import { screenId } from "./screenId.ts";
import { ScreenDetail } from "./ScreenDetail.tsx";

const Wrapper = styled(Box)({
  width: "100%",
  height: "100%",
  display: "flex",
  flexDirection: "column",
  background: "linear-gradient(89.96deg, #2C2B2B 38.71%, #212121 99.96%)",
  boxShadow:
    "inset 0px -1px 0px rgba(0, 0, 0, 0.6), inset 0px -2px 3px 0px rgba(0, 0, 0, 0.4), -1px -2px 0px rgba(0, 0, 0, 0.7), 1px 0px 0 rgba(0, 0, 0, 0.7)",
});

const Scroll = styled(SimpleBar)({ maxHeight: "460px", width: "100%" });

// A sub-level of Settings: rise up from below on open, drop back down on "← Settings",
// distinct from the lateral slide used between top-level views.
const drillDownVariants: Variants = {
  hidden: { opacity: 0, y: 400 },
  show: { opacity: 1, y: 0 },
  exit: { opacity: 0, y: 400 },
};

export const ScreensView = () => {
  const navigate = useNavigate();
  const { screens, error, clearError } = useScreens();
  const [selectedId, setSelectedId] = useState<string | undefined>(undefined);

  const selected = screens.find((s) => screenId(s.screen) === selectedId);

  return (
    <AnimatedLayout
      variants={drillDownVariants}
      style={{
        display: "flex",
        alignItems: "flex-start",
        justifyContent: "center",
        padding: "72px",
        width: "100%",
      }}
    >
      <Wrapper>
        {error && (
          <Alert severity="error" onClose={clearError} sx={{ m: 2 }}>
            {error}
          </Alert>
        )}

        {selected ? (
          <Scroll autoHide={false}>
            <ScreenDetail
              item={selected}
              onBack={() => setSelectedId(undefined)}
              onDeleted={() => setSelectedId(undefined)}
            />
          </Scroll>
        ) : (
          <Scroll autoHide={false}>
            <Button
              color="inherit"
              size="small"
              onClick={() => navigate("/settings")}
              sx={{ m: 1 }}
            >
              ← Settings
            </Button>
            <List sx={{ width: "100%" }}>
              {screens.length === 0 && (
                <Box p={3}>
                  <Typography color="text.secondary">
                    No screens defined.
                  </Typography>
                </Box>
              )}
              {screens.map((entry) => {
                const id = screenId(entry.screen);
                return (
                  <ListItemButton key={id} onClick={() => setSelectedId(id)}>
                    <ListItemText
                      primary={entry.screen.name}
                      secondary={`${id} • ${Math.round(entry.distance)}m`}
                    />
                    {entry.isPlaying && (
                      <Chip label="playing" size="small" color="success" />
                    )}
                  </ListItemButton>
                );
              })}
            </List>
          </Scroll>
        )}
      </Wrapper>
    </AnimatedLayout>
  );
};
