import {
  Divider,
  List,
  ListItem,
  ListItemText,
  Stack,
  styled,
  Typography,
} from "@mui/material";
import SimpleBar from "simplebar-react";
import { MediaPlayerListItemButton } from "../MediaPlayersList/MediaPlayerListItemButton.tsx";
import { MediaPlayer } from "@/models/mediaPlayer.ts";
import { motion } from "framer-motion";
import albumPlaceholder from "@/assets/album-placeholder.jpg";

const AnimatedListItem = motion.create(ListItem);

const Container = styled(Stack)({
  flexBasis: "100%",
  width: "100%",
  maxHeight: "480px",
  background: "linear-gradient(89.96deg, #2C2B2B 38.71%, #212121 99.96%)",
  boxShadow:
    "inset 0px 3px 0px rgba(0,0,0,0.6), 1px 0px 0px rgba(0,0,0,0.7),-1px 0 0 rgba(0,0,0,0.7)",
  boxSizing: "border-box",
  borderRadius: "9px",
  display: "flex",
});

const ScrollBar = styled(SimpleBar)({
  overflowX: "hidden",
  marginTop: "8px",
  maxHeight: "380px",
  display: "flex",
});

export const Queue = ({ queue }: Pick<MediaPlayer, "queue">) => {
  return (
    <Container m={2} width="inherit" direction="column" alignItems="center">
      <Typography sx={{ userSelect: "none" }} mt={2} variant="h5">
        Queue
      </Typography>
      <List sx={{ width: "inherit" }}>
        <ScrollBar autoHide={false}>
          {queue.map((item, index) => (
            <Stack direction="column" key={index} minWidth={0} width="100%">
              <AnimatedListItem
                whileHover={{ scale: 1.05 }}
                whileTap={{ scale: 1.01 }}
              >
                <MediaPlayerListItemButton
                  disableRipple
                  sx={{ padding: "8px" }}
                >
                  <Stack
                    direction="row"
                    gap={2}
                    alignItems="center"
                    minWidth={0}
                    width="100%"
                  >
                    <img
                      src={item.thumbnailUrl || albumPlaceholder}
                      alt={item.title}
                      width="70em"
                      height="70em"
                      style={{
                        borderRadius: "3px",
                        objectFit: "contain",
                        flexShrink: 0,
                      }}
                    />

                    <ListItemText
                      sx={{ minWidth: 0 }}
                      primary={item.title}
                      secondary={item.url}
                      slotProps={{
                        primary: { noWrap: true },
                        secondary: { noWrap: true },
                      }}
                    />
                  </Stack>
                </MediaPlayerListItemButton>
              </AnimatedListItem>
              {index < queue!.length - 1 && (
                <Divider
                  style={{ borderColor: "rgba(0,0,0,0.4)" }}
                  variant="middle"
                />
              )}
            </Stack>
          ))}
        </ScrollBar>
      </List>
    </Container>
  );
};
