/* eslint-disable react-refresh/only-export-components -- test utility module, not consumed by Fast Refresh */
import React, { type ReactElement } from "react";
import { render, type RenderOptions } from "@testing-library/react";
import { Providers } from "@/providers.tsx";

const AllTheProviders = ({ children }: { children: React.ReactNode }) => {
  return <Providers>{children}</Providers>;
};

const customRender = (
  ui: ReactElement,
  options?: Omit<RenderOptions, "wrapper">,
) => render(ui, { wrapper: AllTheProviders, ...options });

export * from "@testing-library/react";
export { customRender as render };
