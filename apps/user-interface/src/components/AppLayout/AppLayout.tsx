import { AnimatePresence, motion } from "framer-motion";
import { Container, Paper, Stack, styled } from "@mui/material";
import { useLocation, useNavigate } from "react-router";
import { isDevelopmentBrowser, post } from "@/utils/misc.ts";
import { NuiEvents } from "@hypnonema/generated-types";
import IconButton from "@components/IconButton/IconButton.tsx";
import CloseIcon from "@mui/icons-material/Close";
import SettingsIcon from "@mui/icons-material/Settings";
import HomeIcon from "@mui/icons-material/Home";
import AnimatedOutlet from "@components/AnimatedOutlet/AnimatedOutlet.tsx";
import { useState } from "react";
import { useNuiEvent } from "@/hooks/useNuiEvent.ts";

const Wrapper = styled(motion.create(Container))(() => ({
  minWidth: "64rem",
  minHeight: "39rem",
}));
const CloseButton = ({ onClick }: { onClick: () => void }) => (
  <IconButton onClick={onClick} tooltip="Close" tooltipPlacement={"bottom"}>
    <CloseIcon sx={{ width: "32px", height: "32px" }} />
  </IconButton>
);

const SettingsButton = ({ onClick }: { onClick: () => void }) => (
  <IconButton onClick={onClick} tooltip="Settings" tooltipPlacement={"left"}>
    <SettingsIcon sx={{ width: "32px", height: "32px" }} />
  </IconButton>
);

const HomeButton = ({ onClick }: { onClick: () => void }) => (
  <IconButton onClick={onClick} tooltip="Home" tooltipPlacement={"left"}>
    <HomeIcon sx={{ width: "32px", height: "32px" }} />
  </IconButton>
);

interface AppHeaderProps {
  onCloseClick: () => void;
  onSettingsClick: () => void;
  onHomeClick: () => void;
}

export const AppHeader = ({
  onCloseClick,
  onSettingsClick,
  onHomeClick,
}: AppHeaderProps) => {
  const location = useLocation();
  return (
    <Stack
      direction="row"
      spacing={1}
      style={{ position: "absolute", top: "3.5%", right: "4%" }}
    >
      {location.pathname === "/" ? (
        <SettingsButton onClick={onSettingsClick} />
      ) : (
        <HomeButton onClick={onHomeClick} />
      )}
      <CloseButton onClick={onCloseClick} />
    </Stack>
  );
};

const Background = styled(motion.create(Paper))(({ theme }) => ({
  background: theme.colors.backgroundGradient,
  position: "relative",
  display: "flex",
  height: "638px",
  overflow: "hidden",
}));

export const AppLayout = () => {
  const [visible, setVisible] = useState(isDevelopmentBrowser);
  const navigate = useNavigate();

  useNuiEvent(NuiEvents.showUi, () => setVisible(true));
  useNuiEvent(NuiEvents.closeUi, () => setVisible(false));

  const closeUi = async () => await post(NuiEvents.closeUi);
  const onSettingsClick = () => {
    navigate("/settings");
  };
  const onHomeClick = () => {
    navigate("/");
  };

  return (
    <AnimatePresence mode="wait">
      {visible && (
        <Wrapper
          maxWidth="md"
          initial={{ y: 1500 }}
          animate={{ y: 200 }}
          transition={{
            type: "spring",
            stiffness: 190,
            damping: 26,
            mass: 1.3,
          }}
          exit={{ y: 1500, opacity: 0 }}
        >
          <Background elevation={8}>
            <AnimatedOutlet />
          </Background>
          <AppHeader
            onSettingsClick={onSettingsClick}
            onCloseClick={closeUi}
            onHomeClick={onHomeClick}
          />
        </Wrapper>
      )}
    </AnimatePresence>
  );
};
