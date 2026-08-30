import { type CSSProperties, type ReactNode, useState } from "react";
import { Box, Stack, styled, Typography } from "@mui/material";
import StyledIconButton from "../IconButton/StyledIconButton.tsx";
import { KeyboardArrowDown } from "@mui/icons-material";
import { AnimatePresence, motion } from "framer-motion";

interface AccordionProps {
  title: string;
  children: ReactNode;
  styles?: CSSProperties;
  isOpen?: boolean;
}

const ChildrenWrapper = styled(motion.create(Box))(() => ({
  background: "linear-gradient(89.96deg, #2C2B2B 38.71%, #212121 99.96%)",
  boxShadow:
    "inset 0px 3px 0px rgba(0,0,0,0.6), 0px 1px 0px #666666, 1px 0px 0px rgba(0,0,0,0.7),-1px 0 0 rgba(0,0,0,0.7)",
  boxSizing: "border-box",
  borderRadius: "9px",
  display: "flex",
  alignItems: "center",
}));

const iconButtonStyles = {
  position: "absolute",
  right: 0,
  top: 0,
};

const Title = styled(Typography)({
  userSelect: "none",
});

const AnimatedIconButton = motion.create(StyledIconButton);
const AnimatedBox = motion.create(Box);
const AnimatedStack = motion.create(Stack);
const AnimatedIcon = motion.create(KeyboardArrowDown);
const animationVariants = {
  open: { height: "auto", scaleY: 1, opacity: 1 },
  collapsed: { height: 0, scaleY: 0, opacity: 0 },
};

export const Accordion = ({
  title,
  isOpen = false,
  children,
  styles,
}: AccordionProps) => {
  const [open, setOpen] = useState(isOpen);
  return (
    <AnimatedStack direction="row" position={"relative"}>
      <Box width="100%">
        <Title variant="overline">{title}</Title>
        <AnimatedBox mt={1} layout width="100%">
          <AnimatePresence mode="sync">
            {open && (
              <motion.div
                key="content"
                initial={open ? "open" : "collapsed"}
                animate="open"
                exit="collapsed"
                variants={animationVariants}
                transition={{
                  duration: 0.5,
                  ease: [0.04, 0.62, 0.23, 0.98],
                }}
              >
                <ChildrenWrapper key="ct2" p={2} sx={styles}>
                  {children}
                </ChildrenWrapper>
              </motion.div>
            )}
          </AnimatePresence>
        </AnimatedBox>
      </Box>

      <AnimatedIconButton
        whileHover={{ scale: 1.1 }}
        whileTap={{ scale: 0.9 }}
        disableRipple
        onClick={() => setOpen(!open)}
        sx={iconButtonStyles}
      >
        <AnimatedIcon
          initial={open ? "closed" : "open"}
          animate={open ? "closed" : "open"}
          variants={{ open: { rotate: 0 }, closed: { rotate: -180 } }}
          transition={{ duration: 0.5 }}
        />
      </AnimatedIconButton>
    </AnimatedStack>
  );
};
