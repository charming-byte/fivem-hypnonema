import { Box, List as MuiList, ListItemText, styled } from "@mui/material";
import SimpleBar from "simplebar-react";

import "simplebar-react/dist/simplebar.min.css";
import { MediaPlayerListItemButton } from "./MediaPlayerListItemButton.tsx";
import { AnimatePresence, motion } from "framer-motion";
import { MediaPlayer } from "@/models/mediaPlayer.ts";
import { ListItem } from "./MediaPlayerListItem.tsx";

interface MediaPlayerListProps {
  items?: Array<MediaPlayer>;
  selectedItem?: MediaPlayer;
  handleSelect: (mediaPlayer: MediaPlayer) => void;
}

const animationVariants = {
  hidden: { opacity: 0 },
  show: {
    opacity: 1,
    transition: {
      staggerChildren: 0.1,
      delayChildren: 0.25,
    },
  },
};

const DisabledItemButton = () => (
  <MediaPlayerListItemButton disabled>
    <ListItemText primary="No MediaPlayers nearby." />
  </MediaPlayerListItemButton>
);

const List = styled(MuiList)({
  display: "flex",
  height: "100%",

  flexDirection: "column",
  alignItems: "flex-start",
  justifyContent: "center",
  padding: 0,
});

const ScrollBar = styled(SimpleBar)({
  overflowX: "hidden",
  maxHeight: "335px",
  width: "100%",
});

const ListItemWrapper = styled(motion.create("div"))({});

export const MediaPlayerList = ({
  items = [],
  selectedItem,
  handleSelect,
}: MediaPlayerListProps) => {
  return (
    <Box height="100%">
      <List>
        <ScrollBar autoHide={false}>
          {items.length >= 1 ? (
            <AnimatePresence>
              <ListItemWrapper
                animate="show"
                initial="hidden"
                variants={animationVariants}
              >
                {items.map((mediaPlayer) => (
                  <ListItem
                    selectedItem={selectedItem}
                    handleSelect={handleSelect}
                    mediaPlayer={mediaPlayer}
                    key={mediaPlayer.handle}
                  />
                ))}
              </ListItemWrapper>
            </AnimatePresence>
          ) : (
            <DisabledItemButton />
          )}
        </ScrollBar>
      </List>
    </Box>
  );
};
