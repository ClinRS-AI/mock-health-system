import { defineConfig } from "vite";
import react from "@vitejs/plugin-react-swc";

export default defineConfig(({ mode }) => {
  const baseConfig = {
    plugins: [react()],
    server: {
      port: 5174
    }
  };

  // Vitest config is picked up from the exported object in test runs;
  // we keep it attached only when running vitest to avoid type issues.
  if (mode === "test") {
    return {
      ...baseConfig,
      test: {
        globals: true,
        environment: "jsdom",
        setupFiles: "./src/test/setup.ts"
      } as any
    } as any;
  }

  return baseConfig as any;
});

