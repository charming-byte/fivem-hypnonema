declare module "*.svg" {
  import type { FC, SVGProps } from "react";
  const src: FC<SVGProps<SVGSVGElement>>;
  export default src;
}
