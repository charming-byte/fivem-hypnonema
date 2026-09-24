import type { CSSProperties, ReactNode } from "react";
import { motion, type Variants } from "framer-motion";

const horizontalVariants: Variants = {
  hidden: { opacity: 0, x: -350 },
  show: { opacity: 1, x: 0 },
  exit: { opacity: 0, x: -650 },
};

interface AnimatedLayoutProps {
  style?: CSSProperties;
  children: ReactNode;
  variants?: Variants;
}

export const AnimatedLayout = ({
  children,
  style,
  variants = horizontalVariants,
}: AnimatedLayoutProps) => {
  return (
    <motion.div
      initial="hidden"
      exit="exit"
      animate="show"
      transition={{ duration: 0.5, type: "tween" }}
      style={{
        width: "100%",
        height: "100%",
        ...style,
      }}
      variants={variants}
    >
      {children}
    </motion.div>
  );
};
